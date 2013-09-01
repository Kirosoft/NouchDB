#!/usr/bin/python
"""
Build LevelDB dlls, build a zip and upload to s3.
Command line arguments:
 -test   : run all tests
 -upload : upload the zip 
"""

import os
import os.path
import shutil
import sys
import time
import re
import json

from util import log, run_cmd_throw, test_for_flag, s3UploadFilePublic, import_boto
from util import s3UploadDataPublic, ensure_s3_doesnt_exist, ensure_path_exists
from util import zip_file_add

# This is version that LevelDB reports. It's in
# http://code.google.com/p/leveldb/source/browse/include/leveldb/db.h
# under kMajorVersion, kMinorVersion
gVersion = "1.2"
# Incrementally increasing revision that identifies my revisions.
# They might happen because of merging new code from LevelDB (they don't always
# update kMinorVersion after changing code) or making changes to my port. 
gRevision = 1

gVer = "%s rev %d" % (gVersion, gRevision)

# The format of release notes is:
# - a list for each version
# - first element of the list is version
# - second element is a date on which the release was made
# - rest are html fragments that will be displayed as <li> items on a html page
gReleaseNotes = [
  ["1.2 rev 1", "2011-??-??",
  "first release",
  "based on <a href='http://code.google.com/p/leveldb/source/detail?r=299ccedfeca1fb3497978c288e76008a5c08e899'>http://code.google.com/p/leveldb/source/detail?r=299ccedfeca1fb3497978c288e76008a5c08e899</a>",
  "<a href='http://kjkpub.s3.amazonaws.com/software/leveldb/rel/LevelDB-1.2-rev-1.zip'>LevelDB-1.2-rev-1.zip</a>"]
]

args = sys.argv[1:]
upload = test_for_flag(args, "-upload") or test_for_flag(args, "upload")
# we force test if we're uploading
test   = test_for_flag(args, "-test") or test_for_flag(args, "test") or upload

def usage():
  print("build.py [-test][-upload]")
  sys.exit(1)

s3_dir = "software/leveldb/rel"

def s3_zip_name():
  return "%s/LevelDB-%s-rev-%d.zip" % (s3_dir, gVersion, gRevision)

def zip_name():
  return "LevelDB-%s-rev-%d.zip" % (gVersion, gRevision)

dll_file = "libleveldb.dll"
dbbench_exe = "db_bench.exe"
test_exes = ["filename_test.exe", "db_test.exe", "corruption_test.exe", "arena_test.exe", "coding_test.exe", "env_test.exe", "memenv_test.exe", "version_edit_test.exe", "c_test.exe", "skiplist_test.exe", "version_set_test.exe", "cache_test.exe", "crc32c_test.exe", "dbformat_test.exe", "log_test.exe", "write_batch_test.exe", "table_test.exe"]

build_files = test_exes + [dll_file] + [dbbench_exe]

def verify_build_ok(build_dir):
  for f in build_files:
    p = os.path.join(build_dir, f)
    ensure_path_exists(p)
    pdb = os.path.splitext(p)[0] + ".pdb"
    ensure_path_exists(pdb)

def run_tests(build_dir):
  total = len(test_exes)
  curr = 1
  for f in test_exes:
    p = os.path.join(build_dir, f)
    print("Running test %d/%d %s" % (curr, total, p))
    out, err = run_cmd_throw(p)
    print(out + err)
    curr += 1

  p = os.path.join(build_dir, dbbench_exe)
  print("Running %s" % p)
  run_cmd_throw(p)

def build_and_test(build_dir, target):
  #shutil.rmtree(build_dir, ignore_errors=True)
  run_cmd_throw("cmd.exe", "/c", "build.bat", target)
  verify_build_ok(build_dir)
  if test: run_tests(build_dir)

def build_zip():
  zip_file_add(zip_name(), "zip-readme.txt", "readme.txt", compress=True, append=True)

  include_path = os.path.join("..", "include", "leveldb")
  include_files = os.listdir(include_path)
  for f in include_files:
    p = os.path.join(include_path, f)
    zippath = "include/leveldb/" + f
    zip_file_add(zip_name(), p, zippath, compress=True, append=True)

  dll_files = ["libleveldb.dll", "libleveldb.lib", "libleveldb.pdb"]
  dll_dir = "rel"
  zip_dir = "32bit"
  for f in dll_files:
    p = os.path.join(dll_dir, f)
    zippath = zip_dir + "/" + f
    zip_file_add(zip_name(), p, zippath, compress=True, append=True)

  dll_dir = "rel64bit"
  zip_dir = "64bit"
  for f in dll_files:
    p = os.path.join(dll_dir, f)
    zippath = zip_dir + "/" + f
    zip_file_add(zip_name(), p, zippath, compress=True, append=True)

def build_s3_js():
  s  = 'var latestVer = "%s";\n' % gVer
  s += 'var builtOn = "%s";\n' % time.strftime("%Y-%m-%d")
  s += 'var zipUrl = "http://kjkpub.s3.amazonaws.com/%s";\n' % s3_zip_name()
  s += 'var relNotes = %s;\n' % json.dumps(gReleaseNotes)
  return s

def upload_to_s3():
  s3UploadFilePublic(zip_name(), s3_zip_name())
  jstxt = build_s3_js()
  s3UploadDataPublic(jstxt, "sumatrapdf/sumatralatest.js")

def main():
  if len(args) != 0:
    usage()
  if upload:
    import_boto()
    ensure_s3_doesnt_exist(s3_zip_name())
  mydir = os.path.dirname(os.path.realpath(__file__))
  print(mydir)
  os.chdir(mydir)

  build_and_test("rel", "Just32rel")
  build_and_test("rel64bit", "Just64rel")
  build_zip()
  if upload: upload_to_s3()

if __name__ == "__main__":
  main()

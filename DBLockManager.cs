using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LevelDB;

namespace NouchDB
{
    public class DBLock
    {
        public DB DB { get; set; }
        public int lockCount { get; set; }

        public DBLock()
        {
            lockCount = 0;
            DB = null;
        }
    }

    // Singleton object to manage access to underlying DB objects
    // TODO: Add multi-thread safety
    public class DBLockManager
    {
        Dictionary<string, DBLock> dbCache = new Dictionary<string, DBLock>();

        public static DBLockManager instance;

        private DBLockManager()
        {

        }

        public static DBLockManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new DBLockManager();
                }
                return instance;
            }
        }

        public DB GetDB(Options options, string filePath)
        {
            DB db = null;

            if (dbCache.ContainsKey(filePath))
            {
                DBLock lockObj = dbCache[filePath];
                // inc the lockcount
                lockObj.lockCount = lockObj.lockCount + 1;
                // save it back in the list
                dbCache[filePath] = lockObj;
                // store the db obj for return
                db = lockObj.DB;
            }
            else
            {

                // create the new DB
                db = new DB(options.GetDBOptions(), filePath);
                // create the new lock object
                DBLock lockObj = new DBLock();
                lockObj.DB = db;
                // store the new lock obj
                dbCache[filePath] = lockObj;
            }

            return db;
        }

        public void Close(string filePath)
        {
            if (dbCache.ContainsKey(filePath))
            {
                DBLock lockObj = dbCache[filePath];
                int lockCount = lockObj.lockCount;
                lockObj.lockCount = lockObj.lockCount - 1;

                if (lockObj.lockCount > 0)
                {
                    dbCache[filePath] = lockObj;
                }
                else
                {
                    dbCache.Remove(filePath);
                    lockObj.DB.Dispose();
                }

            }
            else
            {
                throw new DBNotFoundException("Requested DB not found or already closed: " + filePath);
            }
        }


    }


    public class DBNotFoundException : System.Exception
    {
        public DBNotFoundException(string msg)
            : base(msg)
        {
        }
        public DBNotFoundException(string msg, Exception exception)
            : base(msg, exception)
        {
        }
    }


}

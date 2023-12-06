namespace ServerPatches
{
    public class Logging
    {
        static LoggingInstance _instance;
        public static LoggingInstance Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new LoggingInstance();
                }
                return _instance;
            }
        }

        static LoggingInstance _restLogging;
        public static LoggingInstance Rest
        {
            get
            {
                if (_restLogging == null)
                {
                    _restLogging = new LoggingInstance();
                }
                return _restLogging;
            }
        }

    }
}

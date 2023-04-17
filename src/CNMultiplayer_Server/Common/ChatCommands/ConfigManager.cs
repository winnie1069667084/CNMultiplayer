
namespace ChatCommands
{
    public class ConfigManager
    {
        private static Config config;

        public static void SetConfig(Config config)
        {
            ConfigManager.config = config;
        }

        public static Config GetConfig()
        {
            return ConfigManager.config;
        }
    }
}

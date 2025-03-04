namespace YawGLAPI
{
    public static class ConfigValidator
    {
        /// <summary>
        /// Validates an IP address
        /// </summary>
        public const string IPValidator = @"(\b25[0-5]|\b2[0-4][0-9]|\b[01]?[0-9][0-9]?)(\.(25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)){3}";

        /// <summary>
        /// Validates a port number Range 0-65535
        /// </summary>
        public const string PortRange = @"^([0-9]{1,4}|[1-5][0-9]{4}|6[0-4][0-9]{3}|65[0-4][0-9]{2}|655[0-2][0-9]|6553[0-5])$";
    }
}

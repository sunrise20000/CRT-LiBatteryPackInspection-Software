using CommandLine;

namespace SicUI
{
    public class StartupOptions
    {
        [Option('r', "is-set-rt-ip", 
            Required = false, 
            HelpText = "Set the ip address of the host running SicRT.")]
        public bool RtHostIpAddress { get; set; }
    }
}

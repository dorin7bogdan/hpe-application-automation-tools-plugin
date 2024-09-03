
using HpToolsLauncher.Utils;

namespace HpToolsLauncher.Common
{
    public class UftProps(bool leaveUftOpenIfVisible, DigitalLab digitalLab, UftRunMode? uftRunMode = null, RunAsUser uftRunAsUser = null, bool useUftLicense = false)
    {
        private readonly UftRunMode? _uftRunMode = uftRunMode;
        private readonly DigitalLab _digitalLab = digitalLab;
        private readonly bool _useUftLicense = useUftLicense;
        private readonly bool _leaveUftOpenIfVisible = leaveUftOpenIfVisible;

        public UftRunMode? UftRunMode => _uftRunMode;
        public DigitalLab DigitalLab => _digitalLab;
        public bool UseUftLicense => _useUftLicense;
        public bool LeaveUftOpenIfVisible => _leaveUftOpenIfVisible;
        public RunAsUser UftRunAsUser => uftRunAsUser;
    }
}

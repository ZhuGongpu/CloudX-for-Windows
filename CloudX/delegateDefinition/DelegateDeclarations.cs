using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace CloudX
{
    public class DelegateDeclarations
    {
        public delegate void PopupMessageBox(
            string buttonMessage1, string buttonMessage2, string firstMessage, string secondMessage);

        public delegate void RefreshDeviceUI();

        public delegate void RefreshProgressUI(double progress);

        public delegate void PromptToSaveFile(string fileName, Stream inputStream);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PowerPointPresentation.Transport
{
  internal class ParseProgressStatus
  {
    public PowerPointPresentation.Views.PresentationControl PresentationControl { get; set; }
    public string Message { get; set; }
    public bool IsOnlyMessage { get; set; }
  }
}

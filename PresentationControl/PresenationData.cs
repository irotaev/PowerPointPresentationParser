using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PowerPointPresentation.Control
{
  public class PresentationData
  {
    public PowerPointPresentation.Views.PresentationControl PresentationControl { get; set; }
    
    public string PresentationFullPath { get; set; }

    public string PresentationName { get; set; }

    public object Category { get; set; }

    public string Login { get; set; }
  }
}

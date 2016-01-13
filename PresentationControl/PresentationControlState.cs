using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PowerPointPresentation.PresentationControl
{
  internal enum PresentationControlState
  {
    /// <summary>
    /// Ожидание заполнения полей
    /// </summary>
    WaitingExecution = 0,
    /// <summary>
    /// Парсится
    /// </summary>
    InProgress = 1
  }
}

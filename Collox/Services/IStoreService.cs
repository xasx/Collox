using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Collox.Services;
public interface IStoreService
{
    Task AppendParagraph(string text, DateTime? timestamp);
    Task SaveNow();
}

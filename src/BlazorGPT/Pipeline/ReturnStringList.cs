using System.Text;
using Microsoft.KernelMemory;

namespace BlazorGPT.Pipeline;

public class ReturnStringList : List<string>
{
    public override string ToString()
    {
        return string.Join(",  \n", this);
    }
}

public class ReturnIntegerList : List<int>
{
    public override string ToString()
    {
        return string.Join(",  \n", this);
    }
}

public class ReturnCitationsList : List<Citation>
{
    public override string ToString()
    {
        
        StringBuilder sb = new StringBuilder();
        foreach (var citation in this)
        {
            sb.AppendLine(citation.DocumentId + " - " + citation.SourceUrl + "  ");
        }
        return sb.ToString();

    }
}
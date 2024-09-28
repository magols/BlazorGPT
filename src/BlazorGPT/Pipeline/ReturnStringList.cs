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
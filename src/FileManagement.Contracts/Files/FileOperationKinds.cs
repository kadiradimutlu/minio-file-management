namespace FileManagement.Contracts.Files;

public static class FileOperationKinds
{
    public const string Uploaded = "uploaded";
    public const string Downloaded = "downloaded";
    public const string Deleted = "deleted";

    public static bool IsSupported(
        string operation)
    {
        return operation is
            Uploaded or
            Downloaded or
            Deleted;
    }
}

namespace ChatTheDoc.Server
{
    public class PdfProperties
    {
        public required string FileName { get; set; }
        public bool UpdateRequired { get; set; }
        public DateTime LastImportDate { get; set; }
    }
}

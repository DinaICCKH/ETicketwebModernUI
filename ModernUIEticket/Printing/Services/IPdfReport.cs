namespace ETicketNewUI.Printing.Services
{
    public interface IPdfReport<T>
    {
        byte[] GeneratePdf(T model);
    }
}

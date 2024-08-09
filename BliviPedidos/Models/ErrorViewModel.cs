namespace BliviPedidos.Models
{
    public class ErrorViewModel
    {
        public string RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

        // Adicione esta linha para definir a propriedade ErrorMessage
        public string ErrorMessage { get; set; }
    }
}

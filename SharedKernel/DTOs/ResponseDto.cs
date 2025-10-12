namespace SharedKernel.DTOs
{
    public class ResponseDto<T>
    {
        public bool Success { get; set; }
        public T Data { get; set; } = default!;
        public string Message { get; set; } = string.Empty;
    }
}

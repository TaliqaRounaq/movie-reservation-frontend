namespace CineBook.Models.DTOs
{
    public class ApiResponseWrapper<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
        public object Errors { get; set; }
    }
}
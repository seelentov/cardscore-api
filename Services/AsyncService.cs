namespace cardscore_api.Services
{
    public class AsyncService
    {
        public AsyncService() { }

        public async Task<T> WithTimeout<T>(Task<T> task, TimeSpan timeout)
        {
            using var cts = new CancellationTokenSource();
            cts.CancelAfter(timeout);

            var completedTask = await Task.WhenAny(task, Task.Delay(timeout, cts.Token));

            if (completedTask == task)
            {
                return await task;
            }
            else
            {
                throw new TimeoutException("Время ожидания истекло");
            }
        }
    }
}

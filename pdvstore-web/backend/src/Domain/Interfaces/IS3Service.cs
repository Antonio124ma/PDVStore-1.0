namespace PDVStore.Domain.Interfaces
{
    public interface IS3Service
    {
        Task<string> UploadAsync(string chave, Stream conteudo, string contentType, CancellationToken ct = default);
        Task<Stream> DownloadAsync(string chave, CancellationToken ct = default);
        Task<bool> ExcluirAsync(string chave, CancellationToken ct = default);
        Task<string> ObterUrlPresignedAsync(string chave, CancellationToken ct = default);
    }
}
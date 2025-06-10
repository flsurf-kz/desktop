namespace FlsurfDesktop.Platform;

public interface IScreenCaptureService
{
    Task<byte[]> CapturePrimaryScreenAsync();
}

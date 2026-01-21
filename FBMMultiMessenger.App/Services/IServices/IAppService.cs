namespace FBMMultiMessenger.Services.IServices
{
    internal interface IAppService
    {
        Task CheckForUpdateAsync();
        Task UpdateAndriodApk();
        Task UpdateDesktopExe();
    }
}

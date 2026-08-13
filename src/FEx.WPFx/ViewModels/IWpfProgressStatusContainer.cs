using System.Windows.Shell;

namespace FEx.WPFx.ViewModels;

public interface IWpfProgressStatusContainer : IWpfProgressInfoGet
{
    void SetPrgState(TaskbarItemProgressState value);
}
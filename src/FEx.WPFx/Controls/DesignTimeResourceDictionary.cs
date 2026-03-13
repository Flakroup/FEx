using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Windows;

namespace FEx.WPFx.Controls;

public class DesignTimeResourceDictionary : ResourceDictionary
{
    private readonly ObservableCollection<ResourceDictionary> _noopMergedDictionaries =
        new NoopObservableCollection<ResourceDictionary>();

    public DesignTimeResourceDictionary()
    {
        FieldInfo fieldInfo =
            typeof(ResourceDictionary).GetField("_mergedDictionaries", BindingFlags.Instance | BindingFlags.NonPublic);

        fieldInfo?.SetValue(this, _noopMergedDictionaries);
    }

    private class NoopObservableCollection<T> : ObservableCollection<T>
    {
        protected override void InsertItem(int index, T item)
        {
            if (DesignerProperties.GetIsInDesignMode(new()))
                base.InsertItem(index, item);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using Snuggle.Core.Implementations;
using Snuggle.Core.Models;
using Snuggle.Handlers;

namespace Snuggle.Converters;

public class GameObjectTreeConverter : IValueConverter {
    public object? Convert(object? value, Type targetType, object parameter, CultureInfo culture) => GenerateGameObjectTree();

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException($"{nameof(GameObjectTreeConverter)} only supports converting to");

    private static IReadOnlyList<TreeViewItem> GenerateGameObjectTree() {
        var list = new List<TreeViewItem>();

        SnuggleCore.Instance.Status.SetProgressMax(SnuggleCore.Instance.Collection.Files.Sum(x => x.Value.ObjectInfos.Count));
        var count = 1;
        foreach (var @object in SnuggleCore.Instance.Collection.Files.SelectMany(x => x.Value.GetAllObjects())) {
            SnuggleCore.Instance.Status.SetProgress(count++);
            if ((UnityClassId) @object.ClassId == UnityClassId.GameObject) {
                SnuggleCore.Instance.Status.SetStatus($"Processing {@object.PathId}");
                var node = GenerateGameObjectNode(@object as GameObject, true);
                if (node == null) {
                    continue;
                }

                list.Add(node);
            }
        }

        SnuggleCore.Instance.Status.Reset();

        return list;
    }

    private static TreeViewItem? GenerateGameObjectNode(GameObject? gameObject, bool rootOnly) {
        if (gameObject?.FindComponent(UnityClassId.Transform).Value is not Transform parent ||
            rootOnly && !parent.Parent.IsNull) {
            return null;
        }

        var item = new TreeViewItem {
            Header = gameObject.Name,
            Tag = new SnuggleObject(gameObject),
        };

        foreach (var child in parent.Children) {
            var childNode = GenerateGameObjectNode(child.Value?.GameObject.Value, false);
            if (childNode == null) {
                continue;
            }

            item.Items.Add(childNode);
        }

        return item;
    }
}

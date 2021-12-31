using System;
using System.Windows;

namespace Snuggle.Components.Renderers;

public partial class AudioRenderer {
    public AudioRenderer() {
        InitializeComponent();
    }

    public static string Attribution => "Audio Engine: FMOD Studio by Firelight Technologies Pty Ltd.";

    private void Refresh(object sender, DependencyPropertyChangedEventArgs e) {
        throw new NotImplementedException();
    }
}

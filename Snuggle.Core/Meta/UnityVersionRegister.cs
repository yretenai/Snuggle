using JetBrains.Annotations;
using Snuggle.Core.Models;

namespace Snuggle.Core.Meta;

[PublicAPI]
public static class UnityVersionRegister {
    public static readonly UnityVersion Unity2021_2 = new(2021, 2);
    public static readonly UnityVersion Unity2021_1 = new(2021, 1);
    public static readonly UnityVersion Unity2021 = new(2021);

    public static readonly UnityVersion Unity2020_3_18 = new(2020, 3, 18);
    public static readonly UnityVersion Unity2020_3 = new(2020, 3);
    public static readonly UnityVersion Unity2020_2 = new(2020, 2);
    public static readonly UnityVersion Unity2020_1 = new(2020, 1);
    public static readonly UnityVersion Unity2020 = new(2020);

    public static readonly UnityVersion Unity2019_4 = new(2019, 4);
    public static readonly UnityVersion Unity2019_3 = new(2019, 3);
    public static readonly UnityVersion Unity2019_2 = new(2019, 2);
    public static readonly UnityVersion Unity2019_1 = new(2019, 1);
    public static readonly UnityVersion Unity2019 = new(2019);

    public static readonly UnityVersion Unity2018_4 = new(2018, 4);
    public static readonly UnityVersion Unity2018_3 = new(2018, 3);
    public static readonly UnityVersion Unity2018_2 = new(2018, 2);
    public static readonly UnityVersion Unity2018_1 = new(2018, 1);
    public static readonly UnityVersion Unity2018 = new(2018);

    public static readonly UnityVersion Unity2017_4 = new(2017, 4);
    public static readonly UnityVersion Unity2017_3_1_P = new(2017, 3, 1, UnityBuildType.Patch);
    public static readonly UnityVersion Unity2017_3 = new(2017, 3);
    public static readonly UnityVersion Unity2017_2 = new(2017, 2);
    public static readonly UnityVersion Unity2017_1 = new(2017, 1);
    public static readonly UnityVersion Unity2017 = new(2017);

    public static readonly UnityVersion Unity5_6 = new(5, 6);
    public static readonly UnityVersion Unity5_5 = new(5, 5);
    public static readonly UnityVersion Unity5_4 = new(5, 4);
    public static readonly UnityVersion Unity5_3 = new(5, 3);
    public static readonly UnityVersion Unity5_2 = new(5, 2);
    public static readonly UnityVersion Unity5_1 = new(5, 1);
    public static readonly UnityVersion Unity5 = new(5);
}

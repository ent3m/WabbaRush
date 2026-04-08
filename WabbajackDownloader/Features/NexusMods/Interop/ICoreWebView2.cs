using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace WabbajackDownloader.Features.NexusMods.Interop;

// ─────────────────────────────────────────────────────────────────────────────
// ICoreWebView2 INTERFACE CHAIN
// We only care about ICoreWebView2_4 and ICoreWebView2_13
// ─────────────────────────────────────────────────────────────────────────────

[GeneratedComInterface]
[Guid("76eceacb-0462-4d94-ac83-423a6793775e")]
internal partial interface ICoreWebView2
{
    void _s01(); void _s02(); void _s03(); void _s04(); void _s05();
    void _s06(); void _s07(); void _s08(); void _s09(); void _s10();
    void _s11(); void _s12(); void _s13(); void _s14(); void _s15();
    void _s16(); void _s17(); void _s18(); void _s19(); void _s20();
    void _s21(); void _s22(); void _s23(); void _s24(); void _s25();
    void _s26(); void _s27(); void _s28(); void _s29(); void _s30();
    void _s31(); void _s32(); void _s33(); void _s34(); void _s35();
    void _s36(); void _s37(); void _s38(); void _s39(); void _s40();
    void _s41(); void _s42(); void _s43(); void _s44(); void _s45();
    void _s46(); void _s47(); void _s48(); void _s49(); void _s50();
    void _s51(); void _s52(); void _s53(); void _s54(); void _s55();
    void _s56(); void _s57(); void _s58();
}

[GeneratedComInterface]
[Guid("9E8F0CF8-E670-4B5E-B2BC-73E061E3184C")]
internal partial interface ICoreWebView2_2 : ICoreWebView2
{
    void _2s01(); void _2s02(); void _2s03(); void _2s04();
    void _2s05(); void _2s06(); void _2s07();
}

[GeneratedComInterface]
[Guid("A0D6DF20-3B92-416D-AA0C-437A9C727857")]
internal partial interface ICoreWebView2_3 : ICoreWebView2_2
{
    void _3s01(); void _3s02(); void _3s03(); void _3s04(); void _3s05();
}

[GeneratedComInterface]
[Guid("20d02d59-6df2-42dc-bd06-f98a694b1302")]
internal partial interface ICoreWebView2_4 : ICoreWebView2_3
{
    void AddFrameCreated(
        nint handler,   // ICoreWebView2FrameCreatedEventHandler* — stub param
        out EventRegistrationToken token);
    void RemoveFrameCreated(EventRegistrationToken token);

    void AddDownloadStarting(
        ICoreWebView2DownloadStartingEventHandler handler,
        out EventRegistrationToken token);
    void RemoveDownloadStarting(EventRegistrationToken token);
}

[GeneratedComInterface]
[Guid("bedb11b8-d63c-11eb-b8bc-0242ac130003")]
internal partial interface ICoreWebView2_5 : ICoreWebView2_4
{
    void _5s01(); void _5s02();
}

[GeneratedComInterface]
[Guid("499aadac-d92c-4589-8a75-111bfc167795")]
internal partial interface ICoreWebView2_6 : ICoreWebView2_5
{
    void _6s01();
}

[GeneratedComInterface]
[Guid("79c24d83-09a3-45ae-9418-487f32a58740")]
internal partial interface ICoreWebView2_7 : ICoreWebView2_6
{
    void _7s01();
}

[GeneratedComInterface]
[Guid("E9632730-6E1E-43AB-B7B8-7B2C9E62E094")]
internal partial interface ICoreWebView2_8 : ICoreWebView2_7
{
    void _8s01(); void _8s02(); void _8s03(); void _8s04(); void _8s05();
    void _8s06(); void _8s07();
}

[GeneratedComInterface]
[Guid("4d7b2eab-9fdc-468d-b998-a9260b5ed651")]
internal partial interface ICoreWebView2_9 : ICoreWebView2_8
{
    void _9s01(); void _9s02(); void _9s03(); void _9s04(); void _9s05();
    void _9s06(); void _9s07(); void _9s08(); void _9s09();
}

[GeneratedComInterface]
[Guid("b1690564-6f5a-4983-8e48-31d1143fecdb")]
internal partial interface ICoreWebView2_10 : ICoreWebView2_9
{
    void _10s01(); void _10s02();
}

[GeneratedComInterface]
[Guid("0be78e56-c193-4051-b943-23b460c08bdb")]
internal partial interface ICoreWebView2_11 : ICoreWebView2_10
{
    void _11s01(); void _11s02(); void _11s03();
}

[GeneratedComInterface]
[Guid("35D69927-BCFA-4566-9349-6B3E0D154CAC")]
internal partial interface ICoreWebView2_12 : ICoreWebView2_11
{
    void _12s01(); void _12s02(); void _12s03();
}

[GeneratedComInterface]
[Guid("f75f09a8-667e-4983-88d6-c8773f315e84")]
internal partial interface ICoreWebView2_13 : ICoreWebView2_12
{
    void GetProfile(out ICoreWebView2Profile profile);
}
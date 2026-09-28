using System.Globalization;
using LightSpace.Core;
using LightSpace.Catalog;
using static Fixtures;

internal static class XmpTests
{
    public static void Register(Action<string, Action> test)
    {
        test("XMP metadata roundtrip includes escaped Unicode", () =>
        {
            var state=new PhotoState{Rating=4,Label="Blue",Caption="Zażółć <światło> & 水",Keywords=["mountains","自然"]};
            var result=XmpSidecar.Import(XmpSidecar.Export(state,false).Xml);Check(result.State.Rating==4 && result.State.Caption==state.Caption && result.State.Keywords.SequenceEqual(state.Keywords));
        });
        test("XMP element and alternate-prefix attributes are supported", () =>
        {
            var result=XmpSidecar.Import(Packet("q:Rating='3.0'", "<q:Label>Red</q:Label><c:Exposure2012>0.75</c:Exposure2012>"));
            Check(result.State.Rating==3 && result.State.Label=="Red" && result.State.Develop.Exposure==.75f);
        });
        test("XMP uses namespaces instead of trusting prefixes", () =>
        {
            var xml=Packet("q:Rating='5'","").Replace(XmpSidecar.Xmp.NamespaceName,"urn:unrelated");Check(XmpSidecar.Import(xml).State.Rating==0);
        });
        test("XMP language alternatives prefer x-default", () =>
        {
            var result=XmpSidecar.Import(Packet("","<d:description><r:Alt><r:li xml:lang='pl'>Polski</r:li><r:li xml:lang='x-default'>Default</r:li></r:Alt></d:description>"));Check(result.State.Caption=="Default");
        });
        test("XMP metadata-only import leaves development untouched", () =>
        {
            var source=new PhotoState{Develop=new(){Exposure=2},Masks=[new(){Kind=MaskKind.Brush}]};var target=new PhotoState{Develop=new(){Exposure=-1}};
            var result=XmpSidecar.Import(XmpSidecar.Export(source).Xml,target,new(MetadataOnly:true));Check(result.State.Develop.Exposure==-1 && result.State.Masks.Length==0 && !result.UsedNativeSettings);
        });
        test("XMP native extension preserves complete brush and curve state", () =>
        {
            var source=new PhotoState{Flag=PhotoFlag.Pick,Rating=4,Develop=new(){Channels=new(){Red=new(){Points=[new(0,0),new(.3f,.6f),new(1,1)]}},Grading=new(){Global=new(30,15)}},Masks=[new(){Kind=MaskKind.Brush,Strokes=[new(){Dabs=[new(.2f,.3f),new(.4f,.5f,.8f)]}]}]};
            var result=XmpSidecar.Import(XmpSidecar.Export(source).Xml);Check(result.UsedNativeSettings && PhotoStateEquality.All(source,result.State));
        });
        test("XMP external metadata overrides stale native metadata", () =>
        {
            var xml=XmpSidecar.Export(new PhotoState{Rating=3}).Xml.Replace("xmp:Rating=\"3\"","xmp:Rating=\"5\"");Check(XmpSidecar.Import(xml).State.Rating==5);
        });
        test("XMP Camera Raw mode can ignore its native extension", () =>
        {
            var xml=XmpSidecar.Export(new PhotoState{Develop=new(){Exposure=1}}).Xml.Replace("crs:Exposure2012=\"1\"","crs:Exposure2012=\"2\"");
            var result=XmpSidecar.Import(xml,options:new(PreferLightSpaceSettings:false));Check(!result.UsedNativeSettings && result.State.Develop.Exposure==2);
        });
        test("XMP reports unsupported RAW white balance and mask properties", () =>
        {
            var result=XmpSidecar.Import(Packet("c:Temperature='5600' c:LensProfileEnable='1'",""));
            Check(result.State.Develop.Temperature==0 && result.Warnings.Any(w=>w.Contains("Temperature")) && result.Warnings.Any(w=>w.Contains("LensProfileEnable")));
        });
        test("XMP rejects conflicting duplicate properties", () => Throws(()=>XmpSidecar.Import(Packet("q:Rating='2'","<q:Rating>4</q:Rating>"))));
        test("XMP rejects DTD declarations", () => Throws(()=>XmpSidecar.Import("<!DOCTYPE x [<!ENTITY sample 'value'>]>"+Packet("",""))));
        test("XMP rejects deep XML before DOM construction", () => Throws(()=>XmpSidecar.Import(string.Concat(Enumerable.Repeat("<x>",70))+string.Concat(Enumerable.Repeat("</x>",70)))));
        test("XMP rejects oversized packets", () => Throws(()=>XmpSidecar.Import(new string(' ',XmpSidecar.MaximumBytes+1))));
        test("XMP rejects unknown native settings versions", () =>
        {
            var xml=XmpSidecar.Export(new()).Xml.Replace("ls:SchemaVersion=\"3\"","ls:SchemaVersion=\"999\"");Throws(()=>XmpSidecar.Import(xml));
        });
        test("XMP rejects multiple RDF subjects", () =>
        {
            var xml=Packet("","").Replace("</r:RDF>","<r:Description r:about='other' /></r:RDF>");Throws(()=>XmpSidecar.Import(xml));
        });
        test("XMP malformed and non-finite values are ignored with warnings", () =>
        {
            var result=XmpSidecar.Import(Packet("c:Exposure2012='NaN' c:Contrast2012='bad'",""));Check(result.State.Develop.Exposure==0 && result.Warnings.Length>=2);
        });
        test("XMP numerical parsing is independent of current locale", () =>
        {
            var previous=CultureInfo.CurrentCulture;try{CultureInfo.CurrentCulture=CultureInfo.GetCultureInfo("pl-PL");Check(XmpSidecar.Import(Packet("c:Exposure2012='1.25'","")).State.Develop.Exposure==1.25f);}finally{CultureInfo.CurrentCulture=previous;}
        });
        test("XMP channel curve sequences map to normalized points", () =>
        {
            var result=XmpSidecar.Import(Packet("","<c:ToneCurvePV2012Blue><r:Seq><r:li>0, 0</r:li><r:li>128, 200</r:li><r:li>255, 255</r:li></r:Seq></c:ToneCurvePV2012Blue>"));
            Check(result.State.Develop.Channels.Blue.Points.Length==3 && Math.Abs(result.State.Develop.Channels.Blue.Points[1].Y-200/255f)<.00001);
        });
        test("XMP metadata export does not leak processing extension", () =>
        {
            var xml=XmpSidecar.Export(new PhotoState{Masks=[new(){Kind=MaskKind.Brush}]},false).Xml;Check(!xml.Contains("camera-raw") && !xml.Contains("ls:Settings"));
        });
    }
    private static string Packet(string attributes,string elements)=>$"<r:RDF xmlns:r='{XmpSidecar.Rdf}' xmlns:q='{XmpSidecar.Xmp}' xmlns:c='{XmpSidecar.Crs}' xmlns:d='{XmpSidecar.Dc}'><r:Description r:about='' {attributes}>{elements}</r:Description></r:RDF>";
}

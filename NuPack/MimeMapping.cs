using System;
using System.Collections.Generic;
using System.IO;

namespace NuPack {
    internal static class MimeMapping {
        private static IDictionary<string, string> mimeMappings;

        static MimeMapping() {
            mimeMappings = new Dictionary<string, string>(344, StringComparer.InvariantCultureIgnoreCase);
            BuildMimeMappings();
        }

        internal static string GetMimeMapping(string fileName) {
            if (fileName == null) {
                throw new ArgumentNullException(fileName);
            }

            string contentType = null;
            string extension = Path.GetExtension(fileName);
            if (mimeMappings.TryGetValue(extension, out contentType)) {
                return contentType;
            }
            return mimeMappings[".*"];
        }

        internal static void AddMimeMapping(string extension, string mimeType) {
            mimeMappings.Add(extension, mimeType);
        }

        /// <summary>
        /// Constructs a mapping of file extensions to mime types
        /// Constructed from the mapping in IIS6
        /// </summary>
        private static void BuildMimeMappings() {
            AddMimeMapping(".*", "application/octet-stream");

            AddMimeMapping(".323", "text/h323");

            AddMimeMapping(".aaf", "application/octet-stream");
            AddMimeMapping(".aca", "application/octet-stream");
            AddMimeMapping(".accdb", "application/msaccess");
            AddMimeMapping(".accde", "application/msaccess");
            AddMimeMapping(".accdt", "application/msaccess");
            AddMimeMapping(".acx", "application/internet-property-stream");
            AddMimeMapping(".afm", "application/octet-stream");
            AddMimeMapping(".ai", "application/postscript");
            AddMimeMapping(".aif", "audio/x-aiff");
            AddMimeMapping(".aifc", "audio/aiff");
            AddMimeMapping(".aiff", "audio/aiff");
            AddMimeMapping(".application", "application/x-ms-application");
            AddMimeMapping(".art", "image/x-jg");
            AddMimeMapping(".asd", "application/octet-stream");
            AddMimeMapping(".asf", "video/x-ms-asf");
            AddMimeMapping(".asi", "application/octet-stream");
            AddMimeMapping(".asm", "text/plain");
            AddMimeMapping(".asr", "video/x-ms-asf");
            AddMimeMapping(".asx", "video/x-ms-asf");
            AddMimeMapping(".atom", "application/atom+xml");
            AddMimeMapping(".au", "audio/basic");
            AddMimeMapping(".avi", "video/x-msvideo");
            AddMimeMapping(".axs", "application/olescript");

            AddMimeMapping(".bas", "text/plain");
            AddMimeMapping(".bcpio", "application/x-bcpio");
            AddMimeMapping(".bin", "application/octet-stream");
            AddMimeMapping(".bmp", "image/bmp");

            AddMimeMapping(".c", "text/plain");
            AddMimeMapping(".cab", "application/octet-stream");
            AddMimeMapping(".calx", "application/vnd.ms-office.calx");
            AddMimeMapping(".cat", "application/vnd.ms-pki.seccat");
            AddMimeMapping(".cdf", "application/x-cdf");
            AddMimeMapping(".chm", "application/octet-stream");
            AddMimeMapping(".class", "application/x-java-applet");
            AddMimeMapping(".clp", "application/x-msclip");
            AddMimeMapping(".cmx", "image/x-cmx");
            AddMimeMapping(".cnf", "text/plain");
            AddMimeMapping(".cod", "image/cis-cod");
            AddMimeMapping(".cpio", "application/x-cpio");
            AddMimeMapping(".cpp", "text/plain");
            AddMimeMapping(".crd", "application/x-mscardfile");
            AddMimeMapping(".crl", "application/pkix-crl");
            AddMimeMapping(".crt", "application/x-x509-ca-cert");
            AddMimeMapping(".csh", "application/x-csh");
            AddMimeMapping(".css", "text/css");
            AddMimeMapping(".csv", "application/octet-stream");
            AddMimeMapping(".cur", "application/octet-stream");

            AddMimeMapping(".dcr", "application/x-director");
            AddMimeMapping(".deploy", "application/octet-stream");
            AddMimeMapping(".der", "application/x-x509-ca-cert");
            AddMimeMapping(".dib", "image/bmp");
            AddMimeMapping(".dir", "application/x-director");
            AddMimeMapping(".disco", "text/xml");
            AddMimeMapping(".dll", "application/x-msdownload");
            AddMimeMapping(".dll.config", "text/xml");
            AddMimeMapping(".dlm", "text/dlm");
            AddMimeMapping(".doc", "application/msword");
            AddMimeMapping(".docm", "application/vnd.ms-word.document.macroEnabled.12");
            AddMimeMapping(".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document");
            AddMimeMapping(".dot", "application/msword");
            AddMimeMapping(".dotm", "application/vnd.ms-word.template.macroEnabled.12");
            AddMimeMapping(".dotx", "application/vnd.openxmlformats-officedocument.wordprocessingml.template");
            AddMimeMapping(".dsp", "application/octet-stream");
            AddMimeMapping(".dtd", "text/xml");
            AddMimeMapping(".dvi", "application/x-dvi");
            AddMimeMapping(".dwf", "drawing/x-dwf");
            AddMimeMapping(".dwp", "application/octet-stream");
            AddMimeMapping(".dxr", "application/x-director");

            AddMimeMapping(".eml", "message/rfc822");
            AddMimeMapping(".emz", "application/octet-stream");
            AddMimeMapping(".eot", "application/octet-stream");
            AddMimeMapping(".eps", "application/postscript");
            AddMimeMapping(".etx", "text/x-setext");
            AddMimeMapping(".evy", "application/envoy");
            AddMimeMapping(".exe", "application/octet-stream");
            AddMimeMapping(".exe.config", "text/xml");

            AddMimeMapping(".fdf", "application/vnd.fdf");
            AddMimeMapping(".fif", "application/fractals");
            AddMimeMapping(".fla", "application/octet-stream");
            AddMimeMapping(".flr", "x-world/x-vrml");
            AddMimeMapping(".flv", "video/x-flv");

            AddMimeMapping(".gif", "image/gif");
            AddMimeMapping(".gtar", "application/x-gtar");
            AddMimeMapping(".gz", "application/x-gzip");

            AddMimeMapping(".h", "text/plain");
            AddMimeMapping(".hdf", "application/x-hdf");
            AddMimeMapping(".hdml", "text/x-hdml");
            AddMimeMapping(".hhc", "application/x-oleobject");
            AddMimeMapping(".hhk", "application/octet-stream");
            AddMimeMapping(".hhp", "application/octet-stream");
            AddMimeMapping(".hlp", "application/winhlp");
            AddMimeMapping(".hqx", "application/mac-binhex40");
            AddMimeMapping(".hta", "application/hta");
            AddMimeMapping(".htc", "text/x-component");
            AddMimeMapping(".htm", "text/html");
            AddMimeMapping(".html", "text/html");
            AddMimeMapping(".htt", "text/webviewhtml");
            AddMimeMapping(".hxt", "text/html");

            AddMimeMapping(".ico", "image/x-icon");
            AddMimeMapping(".ics", "application/octet-stream");
            AddMimeMapping(".ief", "image/ief");
            AddMimeMapping(".iii", "application/x-iphone");
            AddMimeMapping(".inf", "application/octet-stream");
            AddMimeMapping(".ins", "application/x-internet-signup");
            AddMimeMapping(".isp", "application/x-internet-signup");

            AddMimeMapping(".IVF", "video/x-ivf");

            AddMimeMapping(".jar", "application/java-archive");
            AddMimeMapping(".java", "application/octet-stream");
            AddMimeMapping(".jck", "application/liquidmotion");
            AddMimeMapping(".jcz", "application/liquidmotion");
            AddMimeMapping(".jfif", "image/pjpeg");
            AddMimeMapping(".jpb", "application/octet-stream");
            AddMimeMapping(".jpe", "image/jpeg");
            AddMimeMapping(".jpeg", "image/jpeg");
            AddMimeMapping(".jpg", "image/jpeg");
            AddMimeMapping(".js", "application/x-javascript");
            AddMimeMapping(".jsx", "text/jscript");

            AddMimeMapping(".latex", "application/x-latex");
            AddMimeMapping(".lit", "application/x-ms-reader");
            AddMimeMapping(".lpk", "application/octet-stream");
            AddMimeMapping(".lsf", "video/x-la-asf");
            AddMimeMapping(".lsx", "video/x-la-asf");
            AddMimeMapping(".lzh", "application/octet-stream");

            AddMimeMapping(".m13", "application/x-msmediaview");
            AddMimeMapping(".m14", "application/x-msmediaview");
            AddMimeMapping(".m1v", "video/mpeg");
            AddMimeMapping(".m3u", "audio/x-mpegurl");
            AddMimeMapping(".man", "application/x-troff-man");
            AddMimeMapping(".manifest", "application/x-ms-manifest");
            AddMimeMapping(".map", "text/plain");
            AddMimeMapping(".mdb", "application/x-msaccess");
            AddMimeMapping(".mdp", "application/octet-stream");
            AddMimeMapping(".me", "application/x-troff-me");
            AddMimeMapping(".mht", "message/rfc822");
            AddMimeMapping(".mhtml", "message/rfc822");
            AddMimeMapping(".mid", "audio/mid");
            AddMimeMapping(".midi", "audio/mid");
            AddMimeMapping(".mix", "application/octet-stream");
            AddMimeMapping(".mmf", "application/x-smaf");
            AddMimeMapping(".mno", "text/xml");
            AddMimeMapping(".mny", "application/x-msmoney");
            AddMimeMapping(".mov", "video/quicktime");
            AddMimeMapping(".movie", "video/x-sgi-movie");
            AddMimeMapping(".mp2", "video/mpeg");
            AddMimeMapping(".mp3", "audio/mpeg");
            AddMimeMapping(".mpa", "video/mpeg");
            AddMimeMapping(".mpe", "video/mpeg");
            AddMimeMapping(".mpeg", "video/mpeg");
            AddMimeMapping(".mpg", "video/mpeg");
            AddMimeMapping(".mpp", "application/vnd.ms-project");
            AddMimeMapping(".mpv2", "video/mpeg");
            AddMimeMapping(".ms", "application/x-troff-ms");
            AddMimeMapping(".msi", "application/octet-stream");
            AddMimeMapping(".mso", "application/octet-stream");
            AddMimeMapping(".mvb", "application/x-msmediaview");
            AddMimeMapping(".mvc", "application/x-miva-compiled");

            AddMimeMapping(".nc", "application/x-netcdf");
            AddMimeMapping(".nsc", "video/x-ms-asf");
            AddMimeMapping(".nws", "message/rfc822");

            AddMimeMapping(".ocx", "application/octet-stream");
            AddMimeMapping(".oda", "application/oda");
            AddMimeMapping(".odc", "text/x-ms-odc");
            AddMimeMapping(".ods", "application/oleobject");
            AddMimeMapping(".one", "application/onenote");
            AddMimeMapping(".onea", "application/onenote");
            AddMimeMapping(".onetoc", "application/onenote");
            AddMimeMapping(".onetoc2", "application/onenote");
            AddMimeMapping(".onetmp", "application/onenote");
            AddMimeMapping(".onepkg", "application/onenote");
            AddMimeMapping(".osdx", "application/opensearchdescription+xml");

            AddMimeMapping(".p10", "application/pkcs10");
            AddMimeMapping(".p12", "application/x-pkcs12");
            AddMimeMapping(".p7b", "application/x-pkcs7-certificates");
            AddMimeMapping(".p7c", "application/pkcs7-mime");
            AddMimeMapping(".p7m", "application/pkcs7-mime");
            AddMimeMapping(".p7r", "application/x-pkcs7-certreqresp");
            AddMimeMapping(".p7s", "application/pkcs7-signature");
            AddMimeMapping(".pbm", "image/x-portable-bitmap");
            AddMimeMapping(".pcx", "application/octet-stream");
            AddMimeMapping(".pcz", "application/octet-stream");
            AddMimeMapping(".pdf", "application/pdf");
            AddMimeMapping(".pfb", "application/octet-stream");
            AddMimeMapping(".pfm", "application/octet-stream");
            AddMimeMapping(".pfx", "application/x-pkcs12");
            AddMimeMapping(".pgm", "image/x-portable-graymap");
            AddMimeMapping(".pko", "application/vnd.ms-pki.pko");
            AddMimeMapping(".pma", "application/x-perfmon");
            AddMimeMapping(".pmc", "application/x-perfmon");
            AddMimeMapping(".pml", "application/x-perfmon");
            AddMimeMapping(".pmr", "application/x-perfmon");
            AddMimeMapping(".pmw", "application/x-perfmon");
            AddMimeMapping(".png", "image/png");
            AddMimeMapping(".pnm", "image/x-portable-anymap");
            AddMimeMapping(".pnz", "image/png");
            AddMimeMapping(".pot", "application/vnd.ms-powerpoint");
            AddMimeMapping(".potm", "application/vnd.ms-powerpoint.template.macroEnabled.12");
            AddMimeMapping(".potx", "application/vnd.openxmlformats-officedocument.presentationml.template");
            AddMimeMapping(".ppam", "application/vnd.ms-powerpoint.addin.macroEnabled.12");
            AddMimeMapping(".ppm", "image/x-portable-pixmap");
            AddMimeMapping(".pps", "application/vnd.ms-powerpoint");
            AddMimeMapping(".ppsm", "application/vnd.ms-powerpoint.slideshow.macroEnabled.12");
            AddMimeMapping(".ppsx", "application/vnd.openxmlformats-officedocument.presentationml.slideshow");
            AddMimeMapping(".ppt", "application/vnd.ms-powerpoint");
            AddMimeMapping(".pptm", "application/vnd.ms-powerpoint.presentation.macroEnabled.12");
            AddMimeMapping(".pptx", "application/vnd.openxmlformats-officedocument.presentationml.presentation");
            AddMimeMapping(".prf", "application/pics-rules");
            AddMimeMapping(".prm", "application/octet-stream");
            AddMimeMapping(".prx", "application/octet-stream");
            AddMimeMapping(".ps", "application/postscript");
            AddMimeMapping(".psd", "application/octet-stream");
            AddMimeMapping(".psm", "application/octet-stream");
            AddMimeMapping(".psp", "application/octet-stream");
            AddMimeMapping(".pub", "application/x-mspublisher");

            AddMimeMapping(".qt", "video/quicktime");
            AddMimeMapping(".qtl", "application/x-quicktimeplayer");
            AddMimeMapping(".qxd", "application/octet-stream");

            AddMimeMapping(".ra", "audio/x-pn-realaudio");
            AddMimeMapping(".ram", "audio/x-pn-realaudio");
            AddMimeMapping(".rar", "application/octet-stream");
            AddMimeMapping(".ras", "image/x-cmu-raster");
            AddMimeMapping(".rf", "image/vnd.rn-realflash");
            AddMimeMapping(".rgb", "image/x-rgb");
            AddMimeMapping(".rm", "application/vnd.rn-realmedia");
            AddMimeMapping(".rmi", "audio/mid");
            AddMimeMapping(".roff", "application/x-troff");
            AddMimeMapping(".rpm", "audio/x-pn-realaudio-plugin");
            AddMimeMapping(".rtf", "application/rtf");
            AddMimeMapping(".rtx", "text/richtext");

            AddMimeMapping(".scd", "application/x-msschedule");
            AddMimeMapping(".sct", "text/scriptlet");
            AddMimeMapping(".sea", "application/octet-stream");
            AddMimeMapping(".setpay", "application/set-payment-initiation");
            AddMimeMapping(".setreg", "application/set-registration-initiation");
            AddMimeMapping(".sgml", "text/sgml");
            AddMimeMapping(".sh", "application/x-sh");
            AddMimeMapping(".shar", "application/x-shar");
            AddMimeMapping(".sit", "application/x-stuffit");
            AddMimeMapping(".sldm", "application/vnd.ms-powerpoint.slide.macroEnabled.12");
            AddMimeMapping(".sldx", "application/vnd.openxmlformats-officedocument.presentationml.slide");
            AddMimeMapping(".smd", "audio/x-smd");
            AddMimeMapping(".smi", "application/octet-stream");
            AddMimeMapping(".smx", "audio/x-smd");
            AddMimeMapping(".smz", "audio/x-smd");
            AddMimeMapping(".snd", "audio/basic");
            AddMimeMapping(".snp", "application/octet-stream");
            AddMimeMapping(".spc", "application/x-pkcs7-certificates");
            AddMimeMapping(".spl", "application/futuresplash");
            AddMimeMapping(".src", "application/x-wais-source");
            AddMimeMapping(".ssm", "application/streamingmedia");
            AddMimeMapping(".sst", "application/vnd.ms-pki.certstore");
            AddMimeMapping(".stl", "application/vnd.ms-pki.stl");
            AddMimeMapping(".sv4cpio", "application/x-sv4cpio");
            AddMimeMapping(".sv4crc", "application/x-sv4crc");
            AddMimeMapping(".swf", "application/x-shockwave-flash");

            AddMimeMapping(".t", "application/x-troff");
            AddMimeMapping(".tar", "application/x-tar");
            AddMimeMapping(".tcl", "application/x-tcl");
            AddMimeMapping(".tex", "application/x-tex");
            AddMimeMapping(".texi", "application/x-texinfo");
            AddMimeMapping(".texinfo", "application/x-texinfo");
            AddMimeMapping(".tgz", "application/x-compressed");
            AddMimeMapping(".thmx", "application/vnd.ms-officetheme");
            AddMimeMapping(".thn", "application/octet-stream");
            AddMimeMapping(".tif", "image/tiff");
            AddMimeMapping(".tiff", "image/tiff");
            AddMimeMapping(".toc", "application/octet-stream");
            AddMimeMapping(".tr", "application/x-troff");
            AddMimeMapping(".trm", "application/x-msterminal");
            AddMimeMapping(".tsv", "text/tab-separated-values");
            AddMimeMapping(".ttf", "application/octet-stream");
            AddMimeMapping(".txt", "text/plain");

            AddMimeMapping(".u32", "application/octet-stream");
            AddMimeMapping(".uls", "text/iuls");
            AddMimeMapping(".ustar", "application/x-ustar");

            AddMimeMapping(".vbs", "text/vbscript");
            AddMimeMapping(".vcf", "text/x-vcard");
            AddMimeMapping(".vcs", "text/plain");
            AddMimeMapping(".vdx", "application/vnd.ms-visio.viewer");
            AddMimeMapping(".vml", "text/xml");
            AddMimeMapping(".vsd", "application/vnd.visio");
            AddMimeMapping(".vss", "application/vnd.visio");
            AddMimeMapping(".vst", "application/vnd.visio");
            AddMimeMapping(".vsto", "application/x-ms-vsto");
            AddMimeMapping(".vsw", "application/vnd.visio");
            AddMimeMapping(".vsx", "application/vnd.visio");
            AddMimeMapping(".vtx", "application/vnd.visio");

            AddMimeMapping(".wav", "audio/wav");
            AddMimeMapping(".wax", "audio/x-ms-wax");
            AddMimeMapping(".wbmp", "image/vnd.wap.wbmp");
            AddMimeMapping(".wcm", "application/vnd.ms-works");
            AddMimeMapping(".wdb", "application/vnd.ms-works");
            AddMimeMapping(".wks", "application/vnd.ms-works");
            AddMimeMapping(".wm", "video/x-ms-wm");
            AddMimeMapping(".wma", "audio/x-ms-wma");
            AddMimeMapping(".wmd", "application/x-ms-wmd");
            AddMimeMapping(".wmf", "application/x-msmetafile");
            AddMimeMapping(".wml", "text/vnd.wap.wml");
            AddMimeMapping(".wmlc", "application/vnd.wap.wmlc");
            AddMimeMapping(".wmls", "text/vnd.wap.wmlscript");
            AddMimeMapping(".wmlsc", "application/vnd.wap.wmlscriptc");
            AddMimeMapping(".wmp", "video/x-ms-wmp");
            AddMimeMapping(".wmv", "video/x-ms-wmv");
            AddMimeMapping(".wmx", "video/x-ms-wmx");
            AddMimeMapping(".wmz", "application/x-ms-wmz");
            AddMimeMapping(".wps", "application/vnd.ms-works");
            AddMimeMapping(".wri", "application/x-mswrite");
            AddMimeMapping(".wrl", "x-world/x-vrml");
            AddMimeMapping(".wrz", "x-world/x-vrml");
            AddMimeMapping(".wsdl", "text/xml");
            AddMimeMapping(".wvx", "video/x-ms-wvx");

            AddMimeMapping(".x", "application/directx");
            AddMimeMapping(".xaf", "x-world/x-vrml");
            AddMimeMapping(".xaml", "application/xaml+xml");
            AddMimeMapping(".xap", "application/x-silverlight-app");
            AddMimeMapping(".xbap", "application/x-ms-xbap");
            AddMimeMapping(".xbm", "image/x-xbitmap");
            AddMimeMapping(".xdr", "text/plain");
            AddMimeMapping(".xla", "application/vnd.ms-excel");
            AddMimeMapping(".xlam", "application/vnd.ms-excel.addin.macroEnabled.12");
            AddMimeMapping(".xlc", "application/vnd.ms-excel");
            AddMimeMapping(".xlm", "application/vnd.ms-excel");
            AddMimeMapping(".xls", "application/vnd.ms-excel");
            AddMimeMapping(".xlsb", "application/vnd.ms-excel.sheet.binary.macroEnabled.12");
            AddMimeMapping(".xlsm", "application/vnd.ms-excel.sheet.macroEnabled.12");
            AddMimeMapping(".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            AddMimeMapping(".xlt", "application/vnd.ms-excel");
            AddMimeMapping(".xltm", "application/vnd.ms-excel.template.macroEnabled.12");
            AddMimeMapping(".xltx", "application/vnd.openxmlformats-officedocument.spreadsheetml.template");
            AddMimeMapping(".xlw", "application/vnd.ms-excel");
            AddMimeMapping(".xml", "text/xml");
            AddMimeMapping(".xof", "x-world/x-vrml");
            AddMimeMapping(".xpm", "image/x-xpixmap");
            AddMimeMapping(".xps", "application/vnd.ms-xpsdocument");
            AddMimeMapping(".xsd", "text/xml");
            AddMimeMapping(".xsf", "text/xml");
            AddMimeMapping(".xsl", "text/xml");
            AddMimeMapping(".xslt", "text/xml");
            AddMimeMapping(".xsn", "application/octet-stream");
            AddMimeMapping(".xtp", "application/octet-stream");
            AddMimeMapping(".xwd", "image/x-xwindowdump");

            AddMimeMapping(".z", "application/x-compress");
            AddMimeMapping(".zip", "application/x-zip-compressed");
        }
    };
}
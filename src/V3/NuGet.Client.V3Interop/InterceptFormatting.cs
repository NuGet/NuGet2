using Newtonsoft.Json.Linq;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace NuGet.Client.V3Shim
{
    internal class InterceptFormatting
    {
        public static XElement MakeFeed(string feedBaseAddress, string method, IEnumerable<JToken> packages, string id)
        {
            return MakeFeed(feedBaseAddress, method, packages, Enumerable.Repeat(id, packages.Count()).ToArray());
        }

        public static XElement MakeFeed(string feedBaseAddress, string method, IEnumerable<JToken> packages, string[] id)
        {
            return MakeFeed(feedBaseAddress, method, packages, id, null);
        }

        public static XElement MakeFeed(string feedBaseAddress, string method, IEnumerable<JToken> packages, string[] id, string nextUrl)
        {
            XNamespace atom = XNamespace.Get(@"http://www.w3.org/2005/Atom");
            XElement feed = new XElement(atom + "feed");
            feed.Add(new XElement(atom + "id", string.Format(CultureInfo.InvariantCulture, "{0}/api/v2/{1}", feedBaseAddress, method)));
            feed.Add(new XElement(atom + "title", method));
            int i = 0;
            foreach (JToken package in packages)
            {
                feed.Add(MakeEntry(feedBaseAddress, id[i++], package));
            }

            if (!String.IsNullOrEmpty(nextUrl))
            {
                var nextLink = new XElement(atom + "link");
                nextLink.SetAttributeValue("rel", "next");
                nextLink.SetAttributeValue("href", nextUrl);

                feed.Add(nextLink);
            }

            return feed;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase")]
        static XElement MakeEntry(string feedBaseAddress, string id, JToken package)
        {
            XNamespace atom = XNamespace.Get(@"http://www.w3.org/2005/Atom");
            XNamespace d = XNamespace.Get(@"http://schemas.microsoft.com/ado/2007/08/dataservices");
            XNamespace m = XNamespace.Get(@"http://schemas.microsoft.com/ado/2007/08/dataservices/metadata");

            XElement entry = new XElement(atom + "entry");

            entry.Add(new XElement(atom + "id", string.Format(CultureInfo.InvariantCulture, "{0}/api/v2/Packages(Id='{1}',Version='{2}')", feedBaseAddress, id, package["version"])));
            entry.Add(new XElement(atom + "title", id));
            entry.Add(new XElement(atom + "author", new XElement(atom + "name", package["authors"] != null ? String.Join(", ", package["authors"].Select(a => a.ToString())) : string.Empty)));

            // the content URL should come from the json
            entry.Add(new XElement(atom + "content",
                new XAttribute("type", "application/zip"),
                new XAttribute("src", FieldOrDefault(package, "nupkgUrl", "http://www.nuget.org"))));

            XElement properties = new XElement(m + "properties");
            entry.Add(properties);

            properties.Add(new XElement(d + "Version", package["version"].ToString()));

            // the following fields should come from the json

            properties.Add(new XElement(d + "Description", FieldOrDefault(package, "description", "no description available")));
            properties.Add(new XElement(d + "IsLatestVersion", new XAttribute(m + "type", "Edm.Boolean"), FieldOrDefault(package, "isLatestVersion", "false")));
            properties.Add(new XElement(d + "IsAbsoluteLatestVersion", new XAttribute(m + "type", "Edm.Boolean"), FieldOrDefault(package, "isAbsoluteLatestVersion", "false")));
            properties.Add(new XElement(d + "IsPrerelease", new XAttribute(m + "type", "Edm.Boolean"), FieldOrDefault(package, "isPrerelease", "false")));
            properties.Add(new XElement(d + "ReportAbuseUrl", FieldOrDefault(package, "reportAbuseUrl", "http://www.nuget.org/")));

            JObject jObjPackage = package as JObject;

            JToken title = null;
            if (jObjPackage.TryGetValue("title", out title))
            {
                properties.Add(new XElement(d + "Title", title.ToString()));
            }

            JToken dependencyGroups;
            if (jObjPackage.TryGetValue("dependencyGroups", out dependencyGroups))
            {
                StringBuilder sb = new StringBuilder();

                foreach (JToken group in dependencyGroups)
                {
                    JObject groupObj = group as JObject;

                    // is this ever populated?
                    string targetFramework = string.Empty;
                    JToken tf;
                    if (groupObj.TryGetValue("targetFramework", out tf))
                    {
                        targetFramework = tf.ToString();
                    }

                    JToken dependencies = null;

                    if (groupObj.TryGetValue("dependencies", out dependencies))
                    {
                        foreach (var dependency in dependencies)
                        {
                            string range = string.Empty;
                            JToken rt = null;
                            if (((JObject)dependency).TryGetValue("range", out rt))
                            {
                                range = rt.ToString();
                            }

                            sb.AppendFormat("{0}:{1}:{2}|", dependency["id"].ToString().ToLowerInvariant(), range, targetFramework);
                        }
                    }

                    if (sb.Length > 0)
                    {
                        sb.Remove(sb.Length - 1, 1);
                    }
                }

                properties.Add(new XElement(d + "Dependencies", sb.ToString()));
            }

            JToken licenseNamesToken = null;
            if (jObjPackage.TryGetValue("licenseNames", out licenseNamesToken))
            {
                properties.Add(new XElement(d + "LicenseNames", String.Join(", ", licenseNamesToken.Select(e => e.ToString()))));
            }

            properties.Add(new XElement(d + "RequireLicenseAcceptance", new XAttribute(m + "type", "Edm.Boolean"), FieldOrDefault(package, "requireLicenseAcceptance", "false")));

            JToken licenseUrlToken = null;
            if (jObjPackage.TryGetValue("licenseUrl", out licenseUrlToken))
            {
                properties.Add(new XElement(d + "LicenseUrl", licenseUrlToken.ToString()));
            }

            // the following properties required for GetUpdates (from the UI)

            // the following properties should come from the json
            if (package["iconUrl"] != null)
            {
                properties.Add(new XElement(d + "IconUrl", package["iconUrl"].ToString()));
            }

            properties.Add(new XElement(d + "DownloadCount", new XAttribute(m + "type", "Edm.Int32"), FieldOrDefault(package, "downloadCount", "0")));
            properties.Add(new XElement(d + "GalleryDetailsUrl", FieldOrDefault(package, "galleryDetailsUrl", string.Empty)));
            properties.Add(new XElement(d + "ProjectUrl", FieldOrDefault(package, "projectUrl", string.Empty)));

            // make sure the date is valid, otherwise odata fails
            var publishedObj = package["published"];
            string published = "1990-01-01T02:04:38.407";

            if (publishedObj != null)
            {
                 published = publishedObj.ToObject<DateTime>().ToString("O", CultureInfo.InvariantCulture);
            }


            properties.Add(new XElement(d + "Published", new XAttribute(m + "type", "Edm.DateTime"), published));


            string strTags = string.Empty;
            JToken tags = null;
            if (jObjPackage.TryGetValue("tags", out tags))
            {
                strTags = String.Join(", ", ((JArray)tags).Select(t => t.ToString()));
                properties.Add(new XElement(d + "Tags", strTags));
            }

            properties.Add(new XElement(d + "ReleaseNotes", FieldOrDefault(package, "releaseNotes", string.Empty)));

            return entry;
        }

        private static string FieldOrDefault(JToken token, string field, string placeHolder)
        {
            return token[field] != null ? token[field].ToString() : placeHolder;
        }

        //  The search service currently returns a slightly different JSON format (this will be fixed)

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "id")]
        public static XElement MakeFeedFromSearch(string source, string feedBaseAddress, string method, IEnumerable<JToken> packages, string id)
        {
            XNamespace atom = XNamespace.Get(@"http://www.w3.org/2005/Atom");
            XElement feed = new XElement(atom + "feed");
            feed.Add(new XElement(atom + "id", string.Format(CultureInfo.InvariantCulture, "{0}/api/v2/{1}", feedBaseAddress, method)));
            feed.Add(new XElement(atom + "title", method));
            foreach (JToken package in packages)
            {
                feed.Add(MakeEntrySearch(source, feedBaseAddress, "", package));
            }
            return feed;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase"), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "id")]
        static XElement MakeEntrySearch(string source, string feedBaseAddress, string id, JToken package)
        {
            XNamespace atom = XNamespace.Get(@"http://www.w3.org/2005/Atom");
            XNamespace d = XNamespace.Get(@"http://schemas.microsoft.com/ado/2007/08/dataservices");
            XNamespace m = XNamespace.Get(@"http://schemas.microsoft.com/ado/2007/08/dataservices/metadata");

            XElement entry = new XElement(atom + "entry");

            string registrationId = package["PackageRegistration"]["Id"].ToString();
            string version = package["Version"].ToString();

            entry.Add(new XElement(atom + "id", string.Format(CultureInfo.InvariantCulture, "{0}/api/v2/Packages(Id='{1}',Version='{2}')", feedBaseAddress, registrationId, version)));
            entry.Add(new XElement(atom + "title", registrationId));
            entry.Add(new XElement(atom + "author", new XElement(atom + "name", package["Authors"].ToString())));

            // the content URL should come from the json
            entry.Add(new XElement(atom + "content",
                new XAttribute("type", "application/zip"),
                new XAttribute("src", string.Format(CultureInfo.InvariantCulture, "{0}/package/{1}/{2}", source, registrationId, version))));

            XElement properties = new XElement(m + "properties");
            entry.Add(properties);

            properties.Add(new XElement(d + "Version", package["Version"].ToString()));

            NuGetVersion nugetVersion = NuGetVersion.Parse(version);

            // the following fields should come from the json
            properties.Add(new XElement(d + "Description", package["Description"].ToString()));
            properties.Add(new XElement(d + "IsLatestVersion", new XAttribute(m + "type", "Edm.Boolean"), package["IsLatestStable"].ToString().ToLowerInvariant()));
            properties.Add(new XElement(d + "IsAbsoluteLatestVersion", new XAttribute(m + "type", "Edm.Boolean"), package["IsLatest"].ToString().ToLowerInvariant()));
            properties.Add(new XElement(d + "IsPrerelease", new XAttribute(m + "type", "Edm.Boolean"), nugetVersion.IsPrerelease.ToString().ToLowerInvariant()));

            JToken flattenedDependencies;
            if (((JObject)package).TryGetValue("FlattenedDependencies", out flattenedDependencies))
            {
                properties.Add(new XElement(d + "Dependencies", flattenedDependencies.ToString()));
            }

            // license information should come from the json
            bool license = false;
            bool.TryParse(package["RequiresLicenseAcceptance"].ToString().ToLowerInvariant(), out license);

            properties.Add(new XElement(d + "RequireLicenseAcceptance", new XAttribute(m + "type", "Edm.Boolean"), license.ToString().ToLowerInvariant()));

            if (license)
            {
                properties.Add(new XElement(d + "LicenseUrl", package["LicenseUrl"].ToString()));
            }

            JObject jObjPackage = (JObject)package;

            JToken iconUrl;
            if (jObjPackage.TryGetValue("IconUrl", out iconUrl))
            {
                properties.Add(new XElement(d + "IconUrl", iconUrl.ToString()));
            }

            string downloadCount = package["PackageRegistration"]["DownloadCount"].ToString();

            DateTime published = DateTime.Parse(package["Published"].ToString(), CultureInfo.InvariantCulture);

            properties.Add(new XElement(d + "DownloadCount", new XAttribute(m + "type", "Edm.Int32"), downloadCount));
            properties.Add(new XElement(d + "GalleryDetailsUrl", FieldOrDefault(package, "GalleryDetailsUrl", "http://tempuri.org/")));
            properties.Add(new XElement(d + "Published", new XAttribute(m + "type", "Edm.DateTime"), published.ToString("O", CultureInfo.InvariantCulture)));
            properties.Add(new XElement(d + "Tags", package["Tags"].ToString()));

            // title is optional, if it is not there the UI uses the Id

            JToken title;
            if (jObjPackage.TryGetValue("Title", out title))
            {
                properties.Add(new XElement(d + "Title", title.ToString()));
            }

            string releaseNotes = package["ReleaseNotes"].ToString();

            if (String.IsNullOrEmpty(releaseNotes) || releaseNotes == "null")
            {
                properties.Add(new XElement(d + "ReleaseNotes", string.Empty));
            }
            else
            {
                properties.Add(new XElement(d + "ReleaseNotes", releaseNotes));
            }

            return entry;
        }

        public static string MakeBatchEntry(string boundaryId, byte[] feed)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(String.Format(CultureInfo.InvariantCulture, "--batchresponse_{0}", boundaryId));
            sb.AppendLine("Content-Type: application/http");
            sb.AppendLine("Content-Transfer-Encoding: binary");
            sb.AppendLine(string.Empty);
            sb.AppendLine("HTTP/1.1 200 OK");
            sb.AppendLine("DataServiceVersion: 2.0;");
            sb.AppendLine("Content-Type: application/atom+xml;type=feed;charset=utf-8");
            sb.AppendLine("X-Content-Type-Options: nosniff");
            sb.AppendLine("Cache-Control: no-cache");
            sb.AppendLine(string.Empty);

            string xml = Encoding.UTF8.GetString(feed);

            sb.AppendLine(xml);

            sb.AppendLine(String.Format(CultureInfo.InvariantCulture, "--batchresponse_{0}--", boundaryId));

            return sb.ToString();
        }
    }
}

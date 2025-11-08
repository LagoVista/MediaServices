// --- BEGIN CODE INDEX META (do not edit) ---
// ContentHash: 15b2864039ab8e59020c3738503e0b9911cf7c6424b9652e91601087169eec16
// IndexVersion: 2
// --- END CODE INDEX META ---
using LagoVista.Core.Attributes;
using LagoVista.Core.Models.UIMetaData;
using System;

namespace LagoVista.MediaServices.Models
{
    [DomainDescriptor]
    public class MediaServicesDomain
    {
        //[DomainDescription(Name: "Device Admin", Description: "Models for working with and creating device configurations.  This includes things such as actions, attributes and state machines.")]
        public const string MediaServices = "MediaServices";

        [DomainDescription(MediaServices)]
        public static DomainDescription MediaServicesDescription
        {
            get
            {
                return new DomainDescription()
                {
                    Description = "A set of classes that can be using for managing media.",
                    DomainType = DomainDescription.DomainTypes.BusinessObject,
                    Name = "Media Services",
                    CurrentVersion = new Core.Models.VersionInfo()
                    {
                        Major = 0,
                        Minor = 8,
                        Build = 001,
                        DateStamp = new DateTime(2016, 12, 20),
                        Revision = 1,
                        ReleaseNotes = "Initial unstable preview"
                    }
                };
            }
        }
    }
}

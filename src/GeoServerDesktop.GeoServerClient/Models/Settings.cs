using Newtonsoft.Json;

namespace GeoServerDesktop.GeoServerClient.Models
{
    /// <summary>
    /// Represents GeoServer global settings
    /// </summary>
    public class GlobalSettings
    {
        /// <summary>
        /// Gets or sets the workspace settings
        /// </summary>
        [JsonProperty("settings")]
        public Settings Settings { get; set; }
    }

    /// <summary>
    /// Represents GeoServer settings
    /// </summary>
    public class Settings
    {
        /// <summary>
        /// Gets or sets the character set
        /// </summary>
        [JsonProperty("charset")]
        public string Charset { get; set; }

        /// <summary>
        /// Gets or sets the number of decimals to use when encoding floating point numbers
        /// </summary>
        [JsonProperty("numDecimals")]
        public int? NumDecimals { get; set; }

        /// <summary>
        /// Gets or sets the online resource URL
        /// </summary>
        [JsonProperty("onlineResource")]
        public string OnlineResource { get; set; }

        /// <summary>
        /// Gets or sets whether verbose messages are enabled
        /// </summary>
        [JsonProperty("verbose")]
        public bool? Verbose { get; set; }

        /// <summary>
        /// Gets or sets whether verbose exception messages are enabled
        /// </summary>
        [JsonProperty("verboseExceptions")]
        public bool? VerboseExceptions { get; set; }

        /// <summary>
        /// Gets or sets the contact information
        /// </summary>
        [JsonProperty("contact")]
        public ContactInfo Contact { get; set; }

        /// <summary>
        /// Gets or sets the proxy base URL
        /// </summary>
        [JsonProperty("proxyBaseUrl")]
        public string ProxyBaseUrl { get; set; }

        /// <summary>
        /// Gets or sets the logging location
        /// </summary>
        [JsonProperty("loggingLocation")]
        public string LoggingLocation { get; set; }

        /// <summary>
        /// Gets or sets whether global services are enabled
        /// </summary>
        [JsonProperty("globalServices")]
        public bool? GlobalServices { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of features to return
        /// </summary>
        [JsonProperty("maxFeatures")]
        public int? MaxFeatures { get; set; }
    }

    /// <summary>
    /// Represents contact information
    /// </summary>
    public class ContactInfo
    {
        /// <summary>
        /// Gets or sets the contact person
        /// </summary>
        [JsonProperty("contactPerson")]
        public string ContactPerson { get; set; }

        /// <summary>
        /// Gets or sets the organization name
        /// </summary>
        [JsonProperty("contactOrganization")]
        public string ContactOrganization { get; set; }

        /// <summary>
        /// Gets or sets the position title
        /// </summary>
        [JsonProperty("contactPosition")]
        public string ContactPosition { get; set; }

        /// <summary>
        /// Gets or sets the address type
        /// </summary>
        [JsonProperty("addressType")]
        public string AddressType { get; set; }

        /// <summary>
        /// Gets or sets the street address
        /// </summary>
        [JsonProperty("address")]
        public string Address { get; set; }

        /// <summary>
        /// Gets or sets the city
        /// </summary>
        [JsonProperty("addressCity")]
        public string AddressCity { get; set; }

        /// <summary>
        /// Gets or sets the state or province
        /// </summary>
        [JsonProperty("addressState")]
        public string AddressState { get; set; }

        /// <summary>
        /// Gets or sets the postal code
        /// </summary>
        [JsonProperty("addressPostalCode")]
        public string AddressPostalCode { get; set; }

        /// <summary>
        /// Gets or sets the country
        /// </summary>
        [JsonProperty("addressCountry")]
        public string AddressCountry { get; set; }

        /// <summary>
        /// Gets or sets the voice telephone number
        /// </summary>
        [JsonProperty("contactVoice")]
        public string ContactVoice { get; set; }

        /// <summary>
        /// Gets or sets the facsimile telephone number
        /// </summary>
        [JsonProperty("contactFacsimile")]
        public string ContactFacsimile { get; set; }

        /// <summary>
        /// Gets or sets the email address
        /// </summary>
        [JsonProperty("contactEmail")]
        public string ContactEmail { get; set; }
    }

    /// <summary>
    /// Wrapper for contact information response
    /// </summary>
    public class ContactInfoWrapper
    {
        /// <summary>
        /// Gets or sets the contact information
        /// </summary>
        [JsonProperty("contact")]
        public ContactInfo Contact { get; set; }
    }
}

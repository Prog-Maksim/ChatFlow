namespace ChatFlow.Models.DB;

public class OtherPersonData
{
    public required string PersonId { get; set; }
    public PublicKeys? PublicKeys { get; set; }
}

public class DeviceId
{
    public required string PublicKey { get; set; }
}

public class PublicKeys
{
    public required DeviceId DeviceId { get; set; }
}
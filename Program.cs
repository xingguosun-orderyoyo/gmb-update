using Google.Apis.Auth.OAuth2;
using Google.Apis.MyBusinessBusinessInformation.v1;
using Google.Apis.MyBusinessBusinessInformation.v1.Data;
using Google.Apis.MyBusinessPlaceActions.v1;
using Google.Apis.MyBusinessPlaceActions.v1.Data;
using Google.Apis.Services;


var credential = GoogleCredential.FromJsonParameters(new JsonCredentialParameters
{
    ClientId = "***.apps.googleusercontent.com",
    ClientSecret = "",
    Type = "authorized_user",
    RefreshToken = ""
});

var initializer = new BaseClientService.Initializer
{
    ApplicationName = "agencydkgmb",
    HttpClientInitializer = credential
};

var myBusinessPlaceActionsService = new MyBusinessPlaceActionsService(initializer);
var myBusinessBusinessInformationService = new MyBusinessBusinessInformationService(initializer);

const int MaxRetries = 100;

var locations = await GetLocationsAsync("accounts/106005025784488374079");

var locationName = locations.First().Name;
var links = await GetPlaceActionLinksAsync(locationName);

var orderingLink = links.FirstOrDefault(l => l.PlaceActionType == "FOOD_ORDERING");

var placeActionLink = new PlaceActionLink
{
    PlaceActionType = "FOOD_ORDERING",
    Uri = $"",
    IsPreferred = true,
    Name = orderingLink?.Name
};

if (orderingLink == null)
{
    await myBusinessPlaceActionsService.Locations.PlaceActionLinks.Create(placeActionLink, locationName).ExecuteAsync();
}

var updateRequest = myBusinessPlaceActionsService.Locations.PlaceActionLinks.Patch(placeActionLink, orderingLink.Name);
updateRequest.UpdateMask = "is_preferred";
try
{
    var result = await updateRequest.ExecuteAsync();
}
catch (Exception e)
{
    Console.WriteLine(e);
    throw;
}


async Task<IEnumerable<Location>> GetLocationsAsync(string accountName, string? nextPageToken = null, int retryCounter = 0)
{
    var listRequest = myBusinessBusinessInformationService.Accounts.Locations.List(accountName);
    listRequest.ReadMask = "name";

    listRequest.PageToken = nextPageToken;
    listRequest.PageSize = 100;

    try
    {
        var response = await listRequest.ExecuteAsync();
        var locations = response.Locations ?? new List<Location>();
        // locations = (await GetVerifiedLocations(locations)).ToList();

        if (response.NextPageToken != null)
        {
            locations = locations
                .Concat(await GetLocationsAsync(accountName, response.NextPageToken))
                .ToList();
        }

        return locations;
    }
    catch (Exception)
    {
        if (retryCounter > MaxRetries)
        {
            throw;
        }

        return await GetLocationsAsync(accountName, nextPageToken, ++retryCounter);
    }
}


async Task<IEnumerable<PlaceActionLink>> GetPlaceActionLinksAsync(string locationName, string? nextPageToken = null, int retryCounter = 0)
{
    var listRequest = myBusinessPlaceActionsService.Locations.PlaceActionLinks.List(locationName);
    
    listRequest.PageToken = nextPageToken;
    listRequest.PageSize = 100;

    try
    {
        var response = await listRequest.ExecuteAsync();
        var placeActionLinks = response.PlaceActionLinks ?? new List<PlaceActionLink>();

        if (response.NextPageToken != null)
        {
            placeActionLinks = placeActionLinks
                .Concat(await GetPlaceActionLinksAsync(locationName, response.NextPageToken))
                .ToList();
        }

        return placeActionLinks;
    }
    catch (Exception)
    {
        if (retryCounter > MaxRetries)
        {
            throw;
        }

        return await GetPlaceActionLinksAsync(locationName, nextPageToken, ++retryCounter);
    }
}

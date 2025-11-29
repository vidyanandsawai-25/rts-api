using NtisPlatform.Application.DTOs;
using NtisPlatform.Application.Interfaces;

namespace NtisPlatform.Application.Services;

public class ServiceManagementService : IServiceManagementService
{
    public async Task<List<ServiceDto>> GetServicesAsync()
    {
        // Return hardcoded services data as per requirements
        var services = new List<ServiceDto>
        {
            new ServiceDto
            {
                Id = 1,
                Link = "/propertySearch",
                Icon = "home",
                Title = "Property Tax",
                Subtext = "Pay your property taxes online, view assessment details, and download receipts securely.",
                Stats = new List<ServiceStatDto>
                {
                    new ServiceStatDto { Label = "Total", Value = "12,345" },
                    new ServiceStatDto { Label = "Paid", Value = "9,876" },
                    new ServiceStatDto { Label = "Remaining", Value = "2,469" }
                }
            },
            new ServiceDto
            {
                Id = 2,
                Link = "/water-tax",
                Icon = "droplet",
                Title = "Water Tax",
                Subtext = "Manage water connection bills, track usage, and make payments for water services.",
                Stats = new List<ServiceStatDto>
                {
                    new ServiceStatDto { Label = "Total", Value = "6,500" },
                    new ServiceStatDto { Label = "Paid", Value = "5,200" },
                    new ServiceStatDto { Label = "Remaining", Value = "1,300" }
                }
            },
            new ServiceDto
            {
                Id = 3,
                Link = "/bajar-parwana",
                Icon = "shopping-bag",
                Title = "Bajar Parwana",
                Subtext = "Apply for market permits, renew licenses, and manage your commercial establishment permissions.",
                Stats = new List<ServiceStatDto>
                {
                    new ServiceStatDto { Label = "Total", Value = "1,200" },
                    new ServiceStatDto { Label = "Paid", Value = "800" },
                    new ServiceStatDto { Label = "Remaining", Value = "400" }
                }
            },
            new ServiceDto
            {
                Id = 4,
                Link = "/birth-death-certificates",
                Icon = "file-text",
                Title = "Birth & Death Certificates",
                Subtext = "Apply for and download birth and death certificates with secure verification.",
                Stats = new List<ServiceStatDto>
                {
                    new ServiceStatDto { Label = "Total", Value = "4,000" },
                    new ServiceStatDto { Label = "Paid", Value = "3,200" },
                    new ServiceStatDto { Label = "Remaining", Value = "800" }
                }
            },
            new ServiceDto
            {
                Id = 5,
                Link = "/garbage-collection",
                Icon = "trash-2",
                Title = "Garbage Collection",
                Subtext = "Schedule waste pickup, report missed collections, and track garbage collection services.",
                Stats = new List<ServiceStatDto>
                {
                    new ServiceStatDto { Label = "Total", Value = "3,500" },
                    new ServiceStatDto { Label = "Paid", Value = "3,100" },
                    new ServiceStatDto { Label = "Remaining", Value = "400" }
                }
            },
            new ServiceDto
            {
                Id = 6,
                Link = "/building-permission",
                Icon = "building-2",
                Title = "Building Permission",
                Subtext = "Submit building plans, track approval status, and obtain construction permits online.",
                Stats = new List<ServiceStatDto>
                {
                    new ServiceStatDto { Label = "Total", Value = "1,800" },
                    new ServiceStatDto { Label = "Paid", Value = "1,400" },
                    new ServiceStatDto { Label = "Remaining", Value = "400" }
                }
            },
            new ServiceDto
            {
                Id = 7,
                Link = "/grievance-redressal",
                Icon = "megaphone",
                Title = "Grievance Redressal",
                Subtext = "File complaints, track resolution status, and provide feedback on municipal services.",
                Stats = new List<ServiceStatDto>
                {
                    new ServiceStatDto { Label = "Total", Value = "900" },
                    new ServiceStatDto { Label = "Paid", Value = "650" },
                    new ServiceStatDto { Label = "Remaining", Value = "250" }
                }
            },
            new ServiceDto
            {
                Id = 8,
                Link = "/rts",
                Icon = "clock",
                Title = "RTS (Right to Services)",
                Subtext = "Access guaranteed time-bound services and track application progress under RTS Act.",
                Stats = new List<ServiceStatDto>
                {
                    new ServiceStatDto { Label = "Total", Value = "2,200" },
                    new ServiceStatDto { Label = "Paid", Value = "1,900" },
                    new ServiceStatDto { Label = "Remaining", Value = "300" }
                }
            },
            new ServiceDto
            {
                Id = 9,
                Link = "/municipal-assets",
                Icon = "landmark",
                Title = "Municipal Assets",
                Subtext = "View public assets, infrastructure details, and upcoming development projects.",
                Stats = new List<ServiceStatDto>
                {
                    new ServiceStatDto { Label = "Total", Value = "5,500" },
                    new ServiceStatDto { Label = "Paid", Value = "4,800" },
                    new ServiceStatDto { Label = "Remaining", Value = "700" }
                }
            }
        };

        return await Task.FromResult(services);
    }
}

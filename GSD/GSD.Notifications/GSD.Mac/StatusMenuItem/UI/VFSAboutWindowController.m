#import "VFSAboutWindowController.h"

@interface VFSAboutWindowController ()

@property (strong) VFSProductInfoFetcher *productInfoFetcher;

@end

@implementation VFSAboutWindowController

- (instancetype)initWithProductInfoFetcher:(VFSProductInfoFetcher *)productInfoFetcher
{
    if (productInfoFetcher == nil)
    {
        self = nil;
    }
    else if (self = [super initWithWindowNibName:@"VFSAboutWindowController"])
    {
        _productInfoFetcher = productInfoFetcher;
    }
    
    return self;
}

- (NSString *)GSDVersion
{
    NSString *version;
    NSError *error;
    if ([self.productInfoFetcher tryGetGSDVersion:&version error:&error])
    {
        return version;
    }
    else
    {
        NSLog(@"Error getting GSD version: %@", [error description]);
        return @"Not available";
    }
}

- (NSString *)gitVersion
{
    NSString *version;
    NSError *error;
    if ([self.productInfoFetcher tryGetGitVersion:&version error:&error])
    {
        return version;
    }
    else
    {
        NSLog(@"Error getting Git version: %@", [error description]);
        return @"Not available";
    }
}

@end

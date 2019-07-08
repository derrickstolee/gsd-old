#import <Foundation/Foundation.h>
#import "VFSMockProductInfoFetcher.h"

@interface VFSMockProductInfoFetcher()

@property (copy) NSString *gitVersion;
@property (copy) NSString *GSDVersion;

@end

@implementation VFSMockProductInfoFetcher

- (instancetype) initWithGitVersion:(NSString *) gitVersion
                   GSDVersion:(NSString *) GSDVersion
{
    if (self = [super init])
    {
        _gitVersion = [gitVersion copy];
        _GSDVersion = [GSDVersion copy];
    }
    
    return self;
}

- (BOOL) tryGetGSDVersion:(NSString *__autoreleasing *) version
                          error:(NSError *__autoreleasing *) error
{
    *version = self.GSDVersion;
    return YES;
}

- (BOOL) tryGetGitVersion:(NSString *__autoreleasing *) version
                    error:(NSError *__autoreleasing *) error
{
    *version = self.gitVersion;
    return YES;
}

@end

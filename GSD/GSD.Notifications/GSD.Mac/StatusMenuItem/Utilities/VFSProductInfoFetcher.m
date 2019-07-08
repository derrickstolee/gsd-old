#import <objc/runtime.h>
#import "VFSProductInfoFetcher.h"

NSString * const GSDPath = @"/usr/local/bin/gvfs";
NSString * const GitPath = @"/usr/local/bin/git";

@interface VFSProductInfoFetcher()

@property (strong, nonnull) VFSProcessRunner *processRunner;

@end

@implementation VFSProductInfoFetcher

- (instancetype)initWithProcessRunner:(VFSProcessRunner *)processRunner
{
    if (processRunner == nil)
    {
        self = nil;
    }
    else if (self = [super init])
    {
        _processRunner = processRunner;
    }
    
    return self;
}

- (BOOL)tryGetGSDVersion:(NSString *__autoreleasing *)version
                         error:(NSError *__autoreleasing *)error
{
    NSParameterAssert(version);
    
    if (![self.processRunner tryRunExecutable:[NSURL fileURLWithPath:GSDPath]
                                         args:@[ @"version" ]
                                       output:version
                                        error:error])
    {
        return NO;
    }
    
    return YES;
}

- (BOOL)tryGetGitVersion:(NSString *__autoreleasing *)version
                   error:(NSError *__autoreleasing *)error
{
    NSParameterAssert(version);
    
    if (![self.processRunner tryRunExecutable:[NSURL fileURLWithPath:GitPath]
                                         args:@[ @"version" ]
                                       output:version
                                        error:error])
    {
        return NO;
    }
    
    return YES;
}

@end

#import <XCTest/XCTest.h>
#import "GSDNotification.h"

@interface GSDNotificationTests : XCTestCase
@end

@implementation GSDNotificationTests

- (void)testCreateNotificationWithMissingIdFails
{
    NSDictionary *message = @{
                              @"Title" : @"foo",
                              @"Message" : @"bar",
                              @"Enlistment" : @"/foo/bar",
                              @"EnlistmentCount" : [NSNumber numberWithLong:0]
                              };
    
    NSError *error;
    GSDNotification *notification;
    
    XCTAssertFalse([GSDNotification tryValidateMessage:message
                                           buildNotification:&notification
                                                       error:&error]);
    XCTAssertNotNil(error);
}

- (void)testCreateNotificationWithInvalidIdFails
{
    NSDictionary *message = @{
                              @"Id" : [NSNumber numberWithLong:32],
                              @"Title" : @"foo",
                              @"Message" : @"bar",
                              @"EnlistmentCount" : [NSNumber numberWithLong:0]
                              };
    
    NSError *error;
    GSDNotification *notification;
    XCTAssertFalse([GSDNotification tryValidateMessage:message
                                           buildNotification:&notification
                                                       error:&error]);
    XCTAssertNotNil(error);
}

- (void)testCreateAutomountNotificationWithValidMessageSucceeds
{
    NSDictionary *message = @{
                              @"Id" : [NSNumber numberWithLong:0],
                              @"EnlistmentCount" : [NSNumber numberWithLong:5]
                              };
    
    NSError *error;
    GSDNotification *notification;
    XCTAssertTrue([GSDNotification tryValidateMessage:message
                                          buildNotification:&notification
                                                      error:&error]);
    XCTAssertTrue([notification.title isEqualToString:@"GSD AutoMount"]);
    XCTAssertTrue([notification.message isEqualToString:@"Attempting to mount 5 GSD repos(s)"]);
    XCTAssertNil(error);
}

- (void)testCreateMountNotificationWithValidMessageSucceeds
{
    NSString *enlistment = @"/Users/foo/bar/foo.bar";
    NSDictionary *message = @{
                              @"Id" : [NSNumber numberWithLong:1],
                              @"Enlistment" : enlistment
                              };
    
    NSError *error;
    GSDNotification *notification;
    XCTAssertTrue([GSDNotification tryValidateMessage:message
                                          buildNotification:&notification
                                                      error:&error]);
    XCTAssertTrue([notification.title isEqualToString:@"GSD AutoMount"]);
    XCTAssertTrue([notification.message containsString:enlistment]);
    XCTAssertNil(error);
}

- (void)testCreateMountNotificationWithMissingEnlistmentFails
{
    NSDictionary *message = @{
                              @"Id" : [NSNumber numberWithLong:1],
                              };
    
    NSError *error;
    GSDNotification *notification;
    XCTAssertFalse([GSDNotification tryValidateMessage:message
                                           buildNotification:&notification
                                                       error:&error]);
    XCTAssertNotNil(error);
}

@end

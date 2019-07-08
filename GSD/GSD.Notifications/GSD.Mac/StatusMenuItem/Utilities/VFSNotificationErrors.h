#import <Foundation/Foundation.h>

NS_ASSUME_NONNULL_BEGIN

extern NSErrorDomain const GSDNotificationErrorDomain;
typedef NS_ERROR_ENUM(GSDNotificationErrorDomain, GSDNotificationErrorCode)
{
    GSDInitError = 200,
    GSDAllocError,
    GSDInvalidMessageIdFormatError,
    GSDUnsupportedMessageError,
    GSDMissingEntitlementInfoError,
    GSDMissingRepoCountError,
    GSDMessageParseError,
    GSDMessageReadError,
};

NS_ASSUME_NONNULL_END

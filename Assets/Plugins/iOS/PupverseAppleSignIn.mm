#import <Foundation/Foundation.h>
#import <UIKit/UIKit.h>
#import <AuthenticationServices/AuthenticationServices.h>

extern "C" void UnitySendMessage(const char *, const char *, const char *);
@interface PupverseAppleRequest : NSObject <ASAuthorizationControllerDelegate, ASAuthorizationControllerPresentationContextProviding>
@property(nonatomic,copy) NSString *receiver;
@property(nonatomic,copy) NSString *requestId;
@property(nonatomic,strong) ASAuthorizationController *controller;
@property(nonatomic,strong) UIWindow *window;
@end
static PupverseAppleRequest *activeRequest;
@implementation PupverseAppleRequest
- (ASPresentationAnchor)presentationAnchorForAuthorizationController:(ASAuthorizationController *)controller { return self.window; }
- (void)finish:(NSString *)status token:(NSString *)token state:(NSString *)state {
    NSDictionary *reply=@{@"request":self.requestId,@"status":status,@"token":token ?: @"",@"state":state ?: @""};
    NSData *data=[NSJSONSerialization dataWithJSONObject:reply options:0 error:nil];
    NSString *json=[[NSString alloc] initWithData:data encoding:NSUTF8StringEncoding];
    UnitySendMessage(self.receiver.UTF8String,"OnAppleAccountResult",json.UTF8String);
    self.controller.delegate=nil;self.controller.presentationContextProvider=nil;self.controller=nil;
    if(activeRequest==self)activeRequest=nil;
}
- (void)authorizationController:(ASAuthorizationController *)controller didCompleteWithAuthorization:(ASAuthorization *)authorization {
    if(![authorization.credential isKindOfClass:[ASAuthorizationAppleIDCredential class]]){[self finish:@"error" token:nil state:nil];return;}
    ASAuthorizationAppleIDCredential *credential=(ASAuthorizationAppleIDCredential *)authorization.credential;
    NSString *token=[[NSString alloc] initWithData:credential.identityToken encoding:NSUTF8StringEncoding];
    [self finish:token.length>0?@"ok":@"error" token:token state:credential.state];
}
- (void)authorizationController:(ASAuthorizationController *)controller didCompleteWithError:(NSError *)error {
    [self finish:([error.domain isEqualToString:ASAuthorizationErrorDomain] && error.code==ASAuthorizationErrorCanceled)?@"cancelled":@"error" token:nil state:nil];
}
@end
extern "C" void PupverseAppleSignIn(const char *receiver,const char *nonceHash,const char *state) {
    NSString *target=[NSString stringWithUTF8String:receiver];NSString *hash=[NSString stringWithUTF8String:nonceHash];NSString *requestId=[NSString stringWithUTF8String:state];
    dispatch_async(dispatch_get_main_queue(),^{
        PupverseAppleRequest *request=[PupverseAppleRequest new];request.receiver=target;request.requestId=requestId;
        if(activeRequest!=nil){[request finish:@"busy" token:nil state:nil];return;}
        for(UIScene *scene in UIApplication.sharedApplication.connectedScenes)
            if(scene.activationState==UISceneActivationStateForegroundActive && [scene isKindOfClass:[UIWindowScene class]])
                for(UIWindow *window in ((UIWindowScene *)scene).windows)if(window.isKeyWindow){request.window=window;break;}
        if(request.window==nil){[request finish:@"error" token:nil state:nil];return;}
        activeRequest=request;
        ASAuthorizationAppleIDRequest *appleRequest=[[ASAuthorizationAppleIDProvider new] createRequest];
        appleRequest.requestedScopes=@[ASAuthorizationScopeEmail];appleRequest.nonce=hash;appleRequest.state=requestId;
        request.controller=[[ASAuthorizationController alloc] initWithAuthorizationRequests:@[appleRequest]];
        request.controller.delegate=request;request.controller.presentationContextProvider=request;
        [request.controller performRequests];
    });
}
extern "C" void PupverseAppleCancel(const char *state) {
    NSString *requestId=[NSString stringWithUTF8String:state];
    dispatch_async(dispatch_get_main_queue(),^{
        if([activeRequest.requestId isEqualToString:requestId])if(@available(iOS 16.0,*))[activeRequest.controller cancel];
    });
}

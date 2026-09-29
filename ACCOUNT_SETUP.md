# PupVerse account setup

Home remains the start screen. Tap **Guest / Account** for sign-in, account creation or password reset. **Continue as Guest** returns to Home without changing your cards, coins or hand. No scene wiring is required.

## Connect email/password

1. Open the [Firebase console](https://console.firebase.google.com/). Select the PupVerse project, or create a dedicated one if none exists. Do not reuse another game's project by accident.
2. In **Authentication → Sign-in method**, enable **Email/Password**. Email-link sign-in is not used here. Configure the project's password policy and reset-email template for your test environment. See [Firebase's password-auth setup](https://firebase.google.com/docs/auth/unity/password-auth).
3. Find the project ID and public Web API key in **Project settings → General**. In Unity select `Assets/Resources/PupverseAccountConfig.asset`, enter both values and leave **Email Enabled** checked. Never enter service-account JSON, Apple private keys or account passwords in this asset.
4. Press Play in Battle3D. Home → Guest / Account → Create Account with a test address you control. Check the user appears in Firebase Authentication. Test sign-out, sign-in, incorrect passwords, offline errors and password reset. Test addresses/passwords used for automated checks are synthetic; no live requests run in those checks.

This implementation uses the [Firebase Authentication REST API](https://firebase.google.com/docs/reference/rest/auth) through UnityWebRequest. It does not install the Firebase Unity SDK and does not consume GoogleService-Info.plist. A missing connection disables online sign-in instead of simulating a successful account.

## Connect Apple on iPhone

Register the iOS app in the same Firebase project with the bundle ID from Unity Player Settings (currently `com.pupverse.mobile`). Configure that app's **Sign in with Apple** capability in the Apple Developer portal and enable Apple under Firebase Authentication. Follow [Firebase's Apple configuration steps](https://firebase.google.com/docs/auth/ios/apple), including any provider credentials and private-email-relay configuration relevant to your setup. Apple credentials belong in the service configuration, never in the game repository.

Then enable **Apple Enabled** in the Unity config asset and export iOS. The post-build step links AuthenticationServices and adds the Sign in with Apple entitlement. Select the matching Apple Developer team and provisioning profile in Xcode. Test on a signed iPhone build with an eligible Apple account. The Editor deliberately disables Apple sign-in.

The native bridge uses a fresh random nonce, sends its SHA-256 hash to Apple, checks callback state, and exchanges the returned identity token plus original nonce with Firebase. Cancellation and repeated taps cannot complete an abandoned request.

## Current limits and release work

This is an identity-only development integration. Sessions are held in memory and expire; relaunching returns to guest. Passwords, Firebase tokens and Apple tokens are not saved in PlayerPrefs. Persistent sign-in requires secure token storage and refresh handling in a later pass.

Cards and coins remain one device-local save shared by guest and signed-in users. Signing in neither uploads nor assigns that save to an account. Before cloud progression, implement explicit save ownership/migration, server-authoritative rewards and account switching rules.

Before public release, finish email verification, account deletion/Apple revocation, provider linking, persistent sessions, privacy/terms screens and Apple button branding. Verify actual Firebase responses, password-policy errors, reset delivery, Apple cancellation/Hide My Email, keyboard behaviour, safe areas and app suspension on devices. Automated tests and native syntax checks do not replace this live validation.

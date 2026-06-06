# Codemagic Publish IPA

This repository uses Unity Cloud Build to create the iOS IPA, then Codemagic to upload that IPA to App Store Connect / TestFlight.

Codemagic workflow:

```txt
publish-unity-cloud-ipa
```

## Required Environment Group

Create a Codemagic environment variable group:

```txt
appstore_credentials
```

Add these secret variables:

```txt
APP_STORE_CONNECT_PRIVATE_KEY
APP_STORE_CONNECT_KEY_IDENTIFIER
APP_STORE_CONNECT_ISSUER_ID
```

The API key should have App Manager permission.

## IPA Input Option A: Download From Unity Cloud

Add this environment variable in Codemagic:

```txt
UNITY_CLOUD_IPA_URL
```

Set it to a direct downloadable URL for the Unity Cloud Build IPA.

## IPA Input Option B: One-off Manual Upload Through Git

Create an `upload/` folder and put the IPA there:

```txt
upload/EightZeroDraft.ipa
```

Then run the workflow. This is less ideal because IPA files are binary and should not normally live in Git.

## Output

Codemagic collects:

```txt
build/ios/ipa/*.ipa
```

Then publishes the IPA to App Store Connect and submits it to TestFlight.

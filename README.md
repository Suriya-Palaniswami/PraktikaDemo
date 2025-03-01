# PraktikaLipsync

### Android Permissions
The application requires the following Android permissions:
- Microphone access for speech recognition
- Camera access for video chat
- Internet access for API communication

## Project Structure

- `Assets/Modules/`
  - `AudioService/` - Handles audio recording and playback
  - `CharacterService/` - Manages the 3D character
  - `EnvironmentService/` - Controls environment switching
  - `GPTService/` - Handles API communication with OpenAI
  - `UIService/` - Manages UI elements and interactions

## Building for Android

1. Set up Android SDK in Unity
2. Configure player settings for Android
3. Ensure all required permissions are set in the manifest
4. Build and deploy to an Android device

## Known Issues

- Camera feed may require orientation adjustment on some Android devices
- Audio playback must be manually stopped when exiting conversations

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a Pull Request

## License

[Your chosen license]

## Acknowledgments

- OpenAI for GPT API
- Google Cloud for Speech-to-Text and Text-to-Speech services
 

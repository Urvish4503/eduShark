import argparse
import logging
import os
import subprocess
from pathlib import Path
from typing import Optional


import whisper

from cleanup import cleanup

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s - %(levelname)s - %(message)s",
    handlers=[logging.StreamHandler()],
)
logger = logging.getLogger(__name__)

# Constants
DEFAULT_MODEL_SIZE = "tiny"
AUDIO_BITRATE = "192k"
# we only use mp3
SUPPORTED_AUDIO_FORMATS = (".mp3")


def generate_audio_from_video(video_path: str, audio_path: str) -> bool:
    """
    Extract audio from a video file using ffmpeg.

    Args:
        video_path: Path to the input video file
        audio_path: Path to save the output audio file

    Returns:
        bool: True if conversion succeeded, False otherwise
    """
    try:
        video = Path(video_path)
        audio = Path(audio_path)

        # checking for video might not be needed if we get if from aws
        if not video.exists():
            logger.error(f"Input video file not found: {video_path}")
            return False

        if audio.suffix.lower() not in SUPPORTED_AUDIO_FORMATS:
            logger.error(f"Unsupported audio format: {audio.suffix}")
            return False

        audio.parent.mkdir(parents=True, exist_ok=True)

        # DO NOT TOUCH THIS
        command = [
            "ffmpeg",
            "-y",
            "-i",
            str(video),
            "-b:a",
            AUDIO_BITRATE,
            "-vn",
            "-loglevel",
            "error",
            str(audio),
        ]

        # running ffmpeg command
        subprocess.run(
            command,
            check=True,
            capture_output=True,
            text=True,
        )

        if not audio.exists():
            logger.error("Audio file not created after conversion")
            return False

        logger.info(f"Successfully created audio file: {audio_path}")
        return True

    except subprocess.CalledProcessError as e:
        logger.error(f"FFmpeg conversion failed: {e.stderr}")
        return False
    except Exception as e:
        logger.error(f"Unexpected error during audio conversion: {str(e)}")
        return False


def generate_transcript(audio_path: str) -> Optional[str]:
    """
    Generate transcript from audio file using Whisper model.

    Args:
        audio_path: Path to the input audio file

    Returns:
        Optional[str]: Generated transcript or None if failed
    """
    try:
        audio = Path(audio_path)
        if not audio.exists():
            logger.error(f"Audio file not found: {audio_path}")
            return None

        logger.info(f"Loading Whisper model ({DEFAULT_MODEL_SIZE})...")
        model = whisper.load_model(DEFAULT_MODEL_SIZE)

        logger.info("Starting transcription...")
        result = model.transcribe(str(audio))

        logger.info("Transcription completed successfully")
        # we just need the text
        return result.get("text")

    except Exception as e:
        logger.error(f"Transcription failed: {str(e)}")
        return None


def save_transcript(transcript: str, output_path: str) -> bool:
    """
    Save transcript text to a file.

    Args:
        transcript: Text content to save
        output_path: Path to the output text file

    Returns:
        bool: True if save succeeded, False otherwise
    """
    try:
        # making output file
        output_file = Path(output_path)
        output_file.parent.mkdir(parents=True, exist_ok=True)

        with open(output_file, "w", encoding="utf-8") as f:
            f.write(transcript)

        logger.info(f"Transcript successfully saved to: {output_path}")
        return True

    except PermissionError:
        logger.error(f"Permission denied for path: {output_path}")
        return False
    except Exception as e:
        logger.error(f"Failed to save transcript: {str(e)}")
        return False


def main():
    """Main processing pipeline with hardcoded values"""
    # Hardcoded configuration
    video_path = "binary_search.mp4"
    audio_path = "output/binary_search_audio.mp3"
    transcript_path = "output/binary_search_transcript.txt"

    logger.info(f"Starting processing for video: {video_path}")

    if not generate_audio_from_video(video_path, audio_path):
        logger.error("Audio extraction failed. Aborting process.")
        return

    transcript = generate_transcript(audio_path)

    if transcript:
        logger.info("\nTranscript Generated:")

        if save_transcript(transcript, transcript_path):
            logger.info(f"\nTranscript saved to: {transcript_path}")
        else:
            logger.warning("Transcript generated but failed to save to file")
        logger.info("\nProcess completed successfully")
        # Cleanup prompt
        try:
            response = input("\nDo you want to delete the temporary files? [y/N]: ").strip().lower()
            if response.startswith('y'):
                cleanup(audio_path, transcript_path)
        except KeyboardInterrupt:
            logger.info("\nOperation cancelled by user")
        except Exception as e:
            logger.error(f"Error during cleanup prompt: {str(e)}")
        
        logger.info("\nProcess completed successfully")
    else:
        logger.error("Failed to generate transcript")



if __name__ == "__main__":
    main()

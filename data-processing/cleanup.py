from pathlib import Path
from logging import Logger

def cleanup(audio_path: str, transcript_path: str, logger: Logger) -> None:
    """
    Remove generated files if they exist.

    Args:
        audio_path: Path to the audio file
        transcript_path: Path to the transcript file
    """
    files_to_clean = [(audio_path, "Audio"), (transcript_path, "Transcript")]

    for file_path, file_type in files_to_clean:
        file = Path(file_path)
        if file.exists():
            try:
                file.unlink()
                logger.info(
                    f"Successfully deleted {file_type.lower()} file: {file_path}"
                )
            except Exception as e:
                logger.error(f"Failed to delete {file_type.lower()} file: {str(e)}")
        else:
            logger.warning(f"{file_type} file not found: {file_path}")

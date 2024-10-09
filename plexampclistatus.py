import requests
import time
import xml.etree.ElementTree as ET
import os

# Constants
URL = "http://PLEXIP:32400/status/sessions?X-Plex-Token=PLEXTOKENHERE"
HEADERS = {'X-Plex-Token': 'PlexTokenHERE'}

# Function to fetch and parse XML data
def fetch_data(url, headers):
    try:
        print(f"Fetching data from: {url}")  # Display the full URL
        response = requests.get(url, headers=headers)
        response.raise_for_status()  # Raises an HTTPError for bad responses
        return response.text
    except requests.exceptions.RequestException as e:
        print(f"\033[91mError fetching data: {e}\033[0m")  # Red text for errors
        return None

# Function to parse XML data and extract device and track information
def parse_xml(xml_data):
    root = ET.fromstring(xml_data)
    devices = {}

    for track in root.findall('.//Track'):
        device_info = track.find('.//Player')
        if device_info is not None:
            device_name = device_info.get('title')
            status = device_info.get('state')

            if device_name not in devices:
                devices[device_name] = {'status': status, 'tracks': []}

            # Extracting track info
            media = track.find('.//Media')
            if media is not None:
                album = track.get('parentTitle', 'Unknown Album')
                artist = track.get('grandparentTitle', 'Unknown Artist')
                thumbnail_url = media.find('.//thumb')  # Get thumbnail URL

                # If artist is "Various Artists", use originalTitle instead
                if artist == 'Various Artists':
                    artist = track.get('originalTitle', 'Unknown Artist')

                # Compute progress
                view_offset = int(track.get('viewOffset', 0))
                duration = int(media.get('duration', 0))
                progress_percentage = (view_offset / duration) * 100 if duration > 0 else 0

                track_info = {
                    'album': album,
                    'track': track.get('title', 'Unknown Track'),
                    'artist': artist,
                    'duration': duration,
                    'progress': progress_percentage,
                    'thumbnail': thumbnail_url.text if thumbnail_url is not None else ''  # Extract thumbnail URL
                }
                devices[device_name]['tracks'].append(track_info)

    return devices

# Function to display device and track information
def display_info(devices):
    os.system('cls' if os.name == 'nt' else 'clear')  # Clear screen

    for device, info in devices.items():
        status_line = f"Status: {info['status'].capitalize()}"
        print(f"\033[1;34mDevice: {device} ({status_line})\033[0m")

        for track in info['tracks']:
            duration_minutes = track['duration'] // 60000
            duration_seconds = (track['duration'] % 60000) // 1000
            duration_str = f"{duration_minutes}:{duration_seconds:02d}"

            print(f"  \033[1;33mTrack:\033[0m {track['track']}")
            print(f"  \033[1;35mArtist:\033[0m {track['artist']}")
            print(f"  \033[1;32mAlbum:\033[0m {track['album']}")
            print(f"  \033[1;36mDuration:\033[0m {duration_str}")

            # Display thumbnail URL if available
            if track['thumbnail']:
                print(f"  \033[1;37mThumbnail:\033[0m {track['thumbnail']}")

            # Progress bar calculation
            progress_bar_length = 40
            progress = int(track['progress'] / 100 * progress_bar_length)
            print(f"  \033[1;37mProgress:\033[0m [{'#' * progress}{'.' * (progress_bar_length - progress)}] \033[1;33m{int(track['progress'])}%\033[0m")

        print()  # Empty line between devices

# Main loop
while True:
    xml_data = fetch_data(URL, HEADERS)
    if xml_data:
        devices = parse_xml(xml_data)
        if devices:
            display_info(devices)
        else:
            print("\033[91mNo device info to display.\033[0m")  # Red text for no data
    else:
        print("\033[91mFailed to retrieve data.\033[0m")  # Red text for failure

    time.sleep(10)  # Refresh every 10 seconds

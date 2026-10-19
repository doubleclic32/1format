#!/bin/sh
set -eu

install_dir="$HOME/Applications/1format.app"
if [ -L "$install_dir" ] || [ ! -d "$install_dir" ]; then
    printf '%s\n' "No ordinary 1format app bundle was found at $install_dir."
    exit 1
fi
resolved_parent=$(CDPATH= cd -- "$HOME/Applications" && pwd -P)
resolved_app=$(CDPATH= cd -- "$install_dir" && pwd -P)
if [ "$resolved_app" != "$resolved_parent/1format.app" ]; then
    printf '%s\n' 'Refusing to remove an app outside the expected Applications directory.'
    exit 1
fi
bundle_id=$(/usr/libexec/PlistBuddy -c 'Print :CFBundleIdentifier' "$install_dir/Contents/Info.plist")
if [ "$bundle_id" != 'com.1format.app' ]; then
    printf '%s\n' 'The bundle is not 1format.'
    exit 1
fi
"$install_dir/Contents/MacOS/1format" --uninstall-cleanup
if [ -L "$HOME/.local/bin/1format" ] && [ "$(readlink "$HOME/.local/bin/1format")" = "$install_dir/Contents/MacOS/1format-cli" ]; then
    rm -- "$HOME/.local/bin/1format"
fi
rm -rf -- "$install_dir"
printf '%s\n' '1format was removed. Your settings and conversion results were kept.'

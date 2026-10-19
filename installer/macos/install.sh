#!/bin/sh
set -eu

source_dir=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd -P)
install_dir="$HOME/Applications/1format.app"
bin_dir="$HOME/.local/bin"
if [ -e "$install_dir" ] || [ -L "$install_dir" ]; then
    printf '%s\n' "An installation already exists at $install_dir. Run uninstall.sh first."
    exit 1
fi
if [ -e "$bin_dir/1format" ] || [ -L "$bin_dir/1format" ]; then
    printf '%s\n' "$bin_dir/1format already exists. Remove or rename it before installing 1format."
    exit 1
fi
test -x "$source_dir/1format.app/Contents/MacOS/1format"
mkdir -p "$HOME/Applications" "$bin_dir"
/usr/bin/ditto "$source_dir/1format.app" "$install_dir"
ln -s "$install_dir/Contents/MacOS/1format-cli" "$bin_dir/1format"
printf '%s\n' "Installed $install_dir" "CLI: $bin_dir/1format (add $bin_dir to PATH if needed)."
printf '%s\n' 'Open 1format from Applications. Grant Accessibility and Finder Automation access when requested.'

#!/bin/sh
set -eu

data_dir=${XDG_DATA_HOME:-"$HOME/.local/share"}
install_dir="$data_dir/1format/application"
desktop_file="$data_dir/applications/com.1format.app.desktop"
python3 - "$data_dir" "$install_dir" <<'PY'
import pathlib
import sys

root, target = map(pathlib.Path, sys.argv[1:])
expected = root.resolve() / '1format' / 'application'
if target.is_symlink() or target.resolve() != expected:
    raise SystemExit('Refusing to remove an installation outside its expected directory.')
if (target / '.1format-package').read_text(encoding='utf-8').strip() != 'com.1format.app':
    raise SystemExit('The directory is not an installation created by 1format.')
PY
"$install_dir/1format" --uninstall-cleanup
if [ -L "$HOME/.local/bin/1format" ] && [ "$(readlink "$HOME/.local/bin/1format")" = "$install_dir/1format-cli" ]; then
    rm -- "$HOME/.local/bin/1format"
fi
if [ -f "$desktop_file" ] && grep -q '^X-1format-Managed=true$' "$desktop_file"; then
    rm -- "$desktop_file"
fi
rm -rf -- "$install_dir"
printf '%s\n' '1format was removed. Your settings and conversion results were kept.'

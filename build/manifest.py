#!/usr/bin/env python3
"""Genera el manifest.json del catálogo de plugins de Jellyfin.

El catálogo de Jellyfin (Dashboard -> Complementos -> Repositorios) espera un
array JSON con un objeto por plugin y una lista de versiones. Cada versión
necesita la URL del ZIP instalable y su checksum **MD5** en hexadecimal, que es
lo que valida el servidor antes de extraer el paquete.

Uso:
    python3 build/manifest.py                 # usa el ZIP de dist/ y meta.json
    python3 build/manifest.py --tag v0.23.0   # fuerza la etiqueta de la release
    python3 build/manifest.py --repo otro/repo

El script conserva las versiones anteriores que ya estuvieran en manifest.json,
de modo que los usuarios con una versión antigua instalada sigan viendo su
entrada y puedan actualizar.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import pathlib
import subprocess
import sys
from datetime import datetime, timezone

ROOT = pathlib.Path(__file__).resolve().parent.parent
META_PATH = ROOT / "build" / "meta.json"
DIST_DIR = ROOT / "dist"
MANIFEST_PATH = ROOT / "manifest.json"
DEFAULT_REPO = "mauspancho/Jellyfin.Plugin.PremiumUI"


def repo_slug() -> str:
    """Devuelve `propietario/repositorio` a partir del remoto de git."""
    try:
        url = subprocess.run(
            ["git", "-C", str(ROOT), "remote", "get-url", "origin"],
            capture_output=True,
            text=True,
            check=True,
        ).stdout.strip()
    except (subprocess.CalledProcessError, FileNotFoundError):
        return DEFAULT_REPO

    if url.startswith("git@github.com:"):
        url = url[len("git@github.com:"):]
    elif "github.com/" in url:
        url = url.split("github.com/", 1)[1]

    return url.removesuffix(".git").strip("/") or DEFAULT_REPO


def release_tag(version: str) -> str:
    """Etiqueta de release para una versión de plugin.

    El ensamblado usa cuatro componentes (0.23.0.0) porque lo exige .NET,
    mientras que las releases del repositorio usan tres (v0.23.0).
    """
    parts = version.split(".")
    if len(parts) == 4 and parts[3] == "0":
        return "v" + ".".join(parts[:3])
    return "v" + version


def md5_of(path: pathlib.Path) -> str:
    digest = hashlib.md5()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def build_entry(meta: dict, repo: str, tag: str) -> dict:
    version = meta["version"]
    zip_name = f"Jellyfin.PremiumUI_{version}.zip"
    zip_path = DIST_DIR / zip_name

    if not zip_path.is_file():
        raise SystemExit(
            f"error: no existe {zip_path}. Ejecuta ./build/package.sh antes que este script."
        )

    timestamp = datetime.fromtimestamp(zip_path.stat().st_mtime, tz=timezone.utc)
    version_entry = {
        "version": version,
        "changelog": meta.get("changelog", ""),
        "targetAbi": meta["targetAbi"],
        "sourceUrl": f"https://github.com/{repo}/releases/download/{tag}/{zip_name}",
        "checksum": md5_of(zip_path),
        "timestamp": timestamp.strftime("%Y-%m-%dT%H:%M:%SZ"),
    }

    entry = {
        "guid": meta["guid"],
        "name": meta["name"],
        "description": meta.get("description", ""),
        "overview": meta.get("overview", ""),
        "owner": meta.get("owner", ""),
        "category": meta.get("category", "General"),
        "versions": [version_entry],
    }

    if meta.get("imageUrl"):
        entry["imageUrl"] = meta["imageUrl"]

    return entry


def load_existing() -> list[dict]:
    if not MANIFEST_PATH.is_file():
        return []

    try:
        data = json.loads(MANIFEST_PATH.read_text(encoding="utf-8"))
    except json.JSONDecodeError:
        print("aviso: manifest.json no era JSON válido; se regenera desde cero", file=sys.stderr)
        return []

    return data if isinstance(data, list) else []


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", help="propietario/repositorio de GitHub")
    parser.add_argument("--tag", help="etiqueta de la release (por defecto v<version>)")
    args = parser.parse_args()

    meta = json.loads(META_PATH.read_text(encoding="utf-8"))
    repo = args.repo or repo_slug()
    tag = args.tag or release_tag(meta["version"])

    entry = build_entry(meta, repo, tag)

    # Se conservan las versiones publicadas antes de esta.
    existing = load_existing()
    for package in existing:
        if package.get("guid") != entry["guid"]:
            continue

        known = {v["version"] for v in entry["versions"]}
        for version in package.get("versions", []):
            if version.get("version") not in known:
                entry["versions"].append(version)

    entry["versions"].sort(key=lambda v: [int(part) for part in v["version"].split(".")], reverse=True)

    MANIFEST_PATH.write_text(json.dumps([entry], ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

    zip_name = f"Jellyfin.PremiumUI_{meta['version']}.zip"
    print(f"==> Manifest  : {MANIFEST_PATH}")
    print(f"==> Versión   : {meta['version']} ({len(entry['versions'])} versión(es))")
    print(f"==> Origen    : {entry['versions'][0]['sourceUrl']}")
    print(f"==> MD5       : {entry['versions'][0]['checksum']}  ({zip_name})")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

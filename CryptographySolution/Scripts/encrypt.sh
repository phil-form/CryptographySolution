#!/usr/bin/env bash
# Usage : ./encrypt.sh -o secrets.bin [-s user|machine] AesKey.txt dbString.txt HttpsCertificate.pem HttpsKey.pem
#
# Équivalent Linux de encrypt.ps1 (DPAPI) basé sur systemd-creds :
#   -s user    (défaut) ≈ CurrentUser  : déchiffrable uniquement par ce compte, sur cette machine.
#   -s machine          ≈ LocalMachine : déchiffrable uniquement sur cette machine (clé hôte /var/lib/systemd/credential.secret
#                                        et TPM2 si présent), mais chiffrement ET déchiffrement exigent root.
# Chaque fichier devient une entrée de configuration nommée d'après son nom sans extension
# (ex. AesKey.txt -> "AesKey"), son contenu (trimé) étant la valeur.
#
# Lecture (vérification) : systemd-creds [--user] decrypt --name=<nom du .bin> output.bin -
set -euo pipefail

usage() { sed -n '2p' "$0" >&2; exit 1; }

scope=user
out=
while getopts "o:s:" opt; do
    case $opt in
        o) out=$OPTARG ;;
        s) scope=$OPTARG ;;
        *) usage ;;
    esac
done
shift $((OPTIND - 1))
[[ -n $out && $# -gt 0 ]] || usage

case $scope in
    user)    flags=(--user) ;;
    machine) flags=() ;;
    *)       usage ;;
esac

# Concatène les fichiers, chacun précédé du marqueur et de son nom, pour pouvoir les re-séparer au déchiffrement
sep='@#@SECRET_START@#@'

# Le clair ne transite que par un pipe : il n'est jamais écrit sur le disque.
# Le nom du credential (vérifié au déchiffrement) est le nom du fichier de sortie.
for f in "$@"; do
    name=$(basename -- "$f")
    printf '%s %s\n' "$sep" "${name%.*}"
    # $(<f) retire les retours à la ligne finaux, sed les espaces de début et de fin (≈ Trim())
    printf '%s\n' "$(<"$f")" | sed -e '1s/^[[:space:]]*//' -e '$s/[[:space:]]*$//'
done | systemd-creds "${flags[@]}" encrypt --name="$(basename -- "$out")" - "$out"

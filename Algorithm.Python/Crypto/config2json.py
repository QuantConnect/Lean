
from __future__ import print_function

import json
import argparse
from configs import configs


def main():
    parser = argparse.ArgumentParser(description='Convert config.py Config Dictionary to JSON')
    parser.add_argument('--json_out', '-j', help='output json file', required=True)
    args = parser.parse_args()

    fh = open(args.json_out,'w')
    json.dump(configs,fh)
    fh.close()

if __name__=='__main__':
    main()

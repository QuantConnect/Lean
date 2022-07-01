#!/bin/bash

echo "Start Rebasing Organization Branches"
git config user.name "$(git log -n 1 --pretty=format:%an)"
git config user.email "$(git log -n 1 --pretty=format:%ae)"

git remote set-branches origin '*'
git checkout -- .
git clean -xqdf

for branch in $(git for-each-ref refs/remotes/origin/* | cut -d"$(printf '\t')" -f2 | cut -b21- | grep ^org-)
do
    echo "Rebasing branch $branch"
    git checkout $branch
    git rebase master
    retVal=$?
    if [ $retVal -eq 0 ]; then
        echo "Pushing branch $branch"
        git push --force-with-lease --set-upstream origin $branch
    else
        echo "Rebase failed branch $branch"
        git rebase --abort
    fi
    git checkout master
    git clean -xqdf
done

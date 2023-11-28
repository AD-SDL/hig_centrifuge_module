#!/bin/bash

session="WEI"

folder="$( cd -- "$(dirname "$0")" >/dev/null 2>&1 ; pwd -P )"

tmux new-session -d -s $session
tmux set -g mouse on

window=0
tmux rename-window -t $session:$window 'redis'
tmux send-keys -t $session:$window 'cd ' $folder C-m
# Start the redis server, or ping if it's already up
if [ "$(redis-cli ping)" != "PONG" ]; then
	tmux send-keys -t $session:$window 'redis-server' C-m
fi

window=1
tmux new-window -t $session:$window -n 'server'
tmux send-keys -t $session:$window 'cd ' $folder C-m
tmux send-keys -t $session:$window 'python3 -m wei.server --workcell ./test_hig_workcell.yaml' C-m

window=2
tmux new-window -t $session:$window -n 'engine'
tmux send-keys -t $session:$window 'cd ' $folder C-m
# Uncomment the following for ROS support
# tmux send-keys -t $session:$window 'source ~/wei_ws/install/setup.bash' C-m
tmux send-keys -t $session:$window 'python3 -m wei.engine --workcell ./test_hig_workcell.yaml' C-m

window=3
tmux new-window -t $session:$window -n 'test_application'
tmux send-keys -t $session:$window 'python3 run_test.py'

tmux attach-session -t $session

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyGame.Managers;
using MyGame.Pathfinding;
using MyGame.Core;

namespace MyGame.Player
{
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;

        private PlayerAnimator playerAnimator;
        private GridManager gridManager;

        private Coroutine movementCoroutine;
        private Queue<Vector2Int> currentPath;

        private Vector2 heldDirection = Vector2.zero;
        private bool isMoving = false;

        private void Awake()
        {
            playerAnimator = GetComponent<PlayerAnimator>();
            gridManager = GridManager.Instance;
        }

        public void SetMovementDirection(Vector2 direction)
        {
            heldDirection = direction;

            if (direction == Vector2.zero && !isMoving)
            {
                playerAnimator.UpdateAnimation(Vector2.zero);
            }

            if (movementCoroutine == null)
            {
                movementCoroutine = StartCoroutine(StepWhileHeld());
            }
        }

        public void StartMoveToPath(Vector2Int startPos, Vector2Int targetPos)
        {
            StopCurrentMovement();

            var path = AStarPathfinding.Instance.FindPath(startPos, targetPos);
            if (path != null && path.Count > 0)
            {
                currentPath = new Queue<Vector2Int>(path);
                movementCoroutine = StartCoroutine(FollowPathRoutine());
            }
            else
            {
                playerAnimator.UpdateAnimation(Vector2.zero);
            }
        }

        public void StopCurrentMovement()
        {
            if (movementCoroutine != null)
            {
                StopCoroutine(movementCoroutine);
                movementCoroutine = null;
            }

            currentPath = null;
            heldDirection = Vector2.zero;
            isMoving = false;
            playerAnimator.UpdateAnimation(Vector2.zero);
        }

        private IEnumerator StepWhileHeld()
        {
            while (true)
            {
                if (!isMoving && heldDirection != Vector2.zero)
                {
                    Vector2Int currentGridPos = gridManager.GetGridCoordinates(transform.position);
                    Vector2Int nextGridPos = currentGridPos + new Vector2Int(
                        Mathf.RoundToInt(heldDirection.x),
                        Mathf.RoundToInt(heldDirection.y)
                    );

                    if (gridManager.IsWalkable(nextGridPos))
                    {
                        isMoving = true;
                        Vector3 targetWorldPosition = gridManager.GetWorldPosition(nextGridPos);
                        Vector2 direction = (targetWorldPosition - transform.position).normalized;
                        playerAnimator.UpdateAnimation(direction);

                        while (Vector3.Distance(transform.position, targetWorldPosition) > 0.01f)
                        {
                            transform.position = Vector3.MoveTowards(
                                transform.position,
                                targetWorldPosition,
                                moveSpeed * Time.deltaTime
                            );
                            yield return null;
                        }

                        transform.position = targetWorldPosition;
                        isMoving = false;
                    }
                    else
                    {
                        playerAnimator.UpdateAnimation(Vector2.zero);
                    }
                }

                if (heldDirection == Vector2.zero && !isMoving)
                {
                    playerAnimator.UpdateAnimation(Vector2.zero);
                }

                yield return null;
            }
        }

        private IEnumerator FollowPathRoutine()
        {
            while (currentPath != null && currentPath.Count > 0)
            {
                Vector2Int nextTilePos = currentPath.Dequeue();
                Vector3 targetWorldPosition = gridManager.GetWorldPosition(nextTilePos);
                Vector3 startPosition = transform.position;
                Vector2 direction = (targetWorldPosition - startPosition).normalized;

                playerAnimator.UpdateAnimation(direction);

                while (Vector3.Distance(transform.position, targetWorldPosition) > 0.05f)
                {
                    transform.position = Vector3.MoveTowards(
                        transform.position,
                        targetWorldPosition,
                        moveSpeed * Time.deltaTime
                    );
                    yield return null;
                }

                transform.position = targetWorldPosition;
            }

            StopCurrentMovement();
        }
    }
}

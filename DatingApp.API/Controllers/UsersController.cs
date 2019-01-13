using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using DatingApp.API.Data;
using DatingApp.API.Dtos;
using DatingApp.API.Helpers;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DatingApp.API.Controllers
{
  [ServiceFilter(typeof(LogUserActivity))]
  [Route("api/[controller]")]
  [ApiController]
  public class UsersController : ControllerBase
  {
    private readonly IDatingRepository _repository;
    private readonly IMapper _mapper;
    public UsersController(IDatingRepository repository, IMapper mapper)
    {
      _mapper = mapper;
      _repository = repository;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers([FromQuery]UserParams userParams)
    {
      var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

      var userFromRepo = await _repository.GetUser(currentUserId, true);

      userParams.UserId = currentUserId;

      if (string.IsNullOrEmpty(userParams.Gender))
      {
        userParams.Gender = userFromRepo.Gender == "male" ? "female" : "male";
      }

      var users = await _repository.GetUsers(userParams);

      var usersToReturn = _mapper.Map<IEnumerable<UserForListDto>>(users);

      Response.AddPagination(users.CurrentPage, users.PageSize, users.TotalCount, users.TotalPages);

      return Ok(usersToReturn);
    }

    [HttpGet("{id}", Name = "GetUser")]
    public async Task<IActionResult> GetUser(int id)
    {
      var isCurrentUser = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value) == id;

      var user = await _repository.GetUser(id, isCurrentUser);

      var userToReturn = _mapper.Map<UserForDetailedDto>(user);

      return Ok(userToReturn);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(int id, UserForUpdateDto userForUpdateDto)
    {
      if (id != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
        return Unauthorized();

      var userFromRepo = await _repository.GetUser(id, true);

      _mapper.Map(userForUpdateDto, userFromRepo);

      if (await _repository.SaveAll())
        return NoContent();

      throw new Exception($"Updating user {id} failed on save");
    }

    [HttpPost("{id}/like/{recepientId}")]
    public async Task<IActionResult> LikeUser(int id, int recepientId)
    {
      if (id != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
        return Unauthorized();

      var like = await _repository.GetLike(id, recepientId);

      if (like != null)
        return BadRequest("You already like this user");

      if (await _repository.GetUser(recepientId, false) == null)
        return NotFound();

      like = new Like
      {
        LikerId = id,
        LikeeId = recepientId
      };

      _repository.Add<Like>(like);

      if (await _repository.SaveAll())
        return Ok();

      return BadRequest("Failed to like user");
    }
  }
}